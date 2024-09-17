using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Windows;
using System.IO.Ports;
using CableAssemblyTesterArduinoDue.Commands;
using CableAssemblyTesterArduinoDue.Services;
using Newtonsoft.Json;
using System.IO;
using CableAssemblyTesterArduinoDue.Properties;
using System.Configuration;
using System.Windows.Controls;



namespace CableAssemblyTesterArduinoDue.ViewModels
{
	public class CableTesterViewModel : INotifyPropertyChanged, IDisposable
	{
		private readonly ISerialPortService _serialPortService;
		private bool _isConnected;
		private string _connectButtonText = "Connect";
		private string _displayText = "";
		private string _selectedComPort = string.Empty;
		private readonly Timer _refreshTimer;
		private string _receivedDataBuffer = "";

		public ObservableCollection<string> AvailableComPorts { get; }

		private ObservableCollection<string> _availableCommands;
		public ObservableCollection<string> AvailableCommands
		{
			get => _availableCommands;
			set => SetProperty(ref _availableCommands, value);
		}

		private string _selectedCommand;
		public string SelectedCommand
		{
			get => _selectedCommand;
			set => SetProperty(ref _selectedCommand, value);
		}
		public string SelectedComPort
		{
			get => _selectedComPort;
			set
			{
				if (SetProperty(ref _selectedComPort, value))
				{
					_refreshTimer.Enabled = string.IsNullOrEmpty(value);
					if (string.IsNullOrEmpty(value))
						RefreshAvailablePorts();
				}
			}
		}

		public bool IsNotConnected => !_isConnected;

		public string ConnectButtonText
		{
			get => _connectButtonText;
			set => SetProperty(ref _connectButtonText, value);
		}

		public string DisplayText
		{
			get => _displayText;
			set
			{
				if (_displayText != value)
				{
					_displayText = value;
					OnPropertyChanged(nameof(DisplayText));
					ScrollToEnd();
				}
			}
		}
		private void ScrollToEnd()
		{
			Application.Current.Dispatcher.InvokeAsync(async () =>
			{
				var textBox = Application.Current.MainWindow.FindName("DisplayTextBox") as TextBox;
				if (textBox != null)
				{
					// Wait for the UI to update
					await Task.Delay(10);

					textBox.CaretIndex = textBox.Text.Length;
					textBox.ScrollToEnd();
					textBox.Focus();
				}
			}, System.Windows.Threading.DispatcherPriority.Background);
		}


		public CableTesterViewModel(ISerialPortService serialPortService)
		{
			_serialPortService = serialPortService;
			_serialPortService.DataReceived += SerialPortService_DataReceived;
			AvailableCommands = new ObservableCollection<string>();
			LoadCommandsFromSettings();
			AvailableComPorts = new ObservableCollection<string>();

			ConnectCommand = new RelayCommand(ExecuteConnect, () => !string.IsNullOrEmpty(SelectedComPort));
			SendCommand = new RelayCommand(ExecuteSend, () => _isConnected && SelectedCommand != null);
			VersionCommand = new RelayCommand(ExecuteVersion, () => _isConnected);
			TestCommand = new RelayCommand(ExecuteTest, () => _isConnected);
			LearnCommand = new RelayCommand(ExecuteLearn, () => _isConnected);
			SaveCommand = new RelayCommand(ExecuteSave, () => _isConnected);
			ShowCommand = new RelayCommand(ExecuteShow, () => _isConnected);

			_refreshTimer = new Timer(5000) { AutoReset = true };
			_refreshTimer.Elapsed += (_, _) => RefreshAvailablePorts();
			_refreshTimer.Start();
			Initialize();
		}

		private void Initialize()
		{
			RefreshAvailablePorts();
			//AppendToDisplay("CableTester initialized.");
		}


		private void LoadCommandsFromSettings()
		{
			try
			{
				AvailableCommands = new ObservableCollection<string>();

				// Get all properties from Settings

				string ShowCommand = Settings.Default.CableTesterShow;
				string DeleteCommand = Settings.Default.CableTesterDelete;
				string InvalidCommand = Settings.Default.CableTesterInvalid;
				string SaveCommand = Settings.Default.CableTesterSave;
				string TestCommand = Settings.Default.CableTesterTest;
				string LearnCommand = Settings.Default.CableTesterLearn;
				string VersionCommand = Settings.Default.CableTesterVersion;

				AvailableCommands.Add(ShowCommand);
				AvailableCommands.Add(DeleteCommand);
				AvailableCommands.Add(ShowCommand);
				AvailableCommands.Add(InvalidCommand);
				AvailableCommands.Add(SaveCommand);
				AvailableCommands.Add(TestCommand);
				AvailableCommands.Add(VersionCommand);

				//Console.WriteLine($"Loaded {AvailableCommands.Count} commands"); // Debug output

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading commands from settings: {ex.Message}");
				AvailableCommands = new ObservableCollection<string>();
			}
			OnPropertyChanged(nameof(AvailableCommands)); // Notify UI of changes

		}

		private void RefreshAvailablePorts()
		{
			if (string.IsNullOrEmpty(SelectedComPort))
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					AvailableComPorts.Clear();
					foreach (var port in _serialPortService.GetAvailablePorts())
					{
						AvailableComPorts.Add(port);
					}
				});
			}
		}




		#region Commands
		public ICommand ConnectCommand { get; }
		public ICommand SendCommand { get; }
		public ICommand ShowCommand { get; }
		public ICommand VersionCommand { get; }
		public ICommand LearnCommand { get; }
		public ICommand SaveCommand { get; }
		public ICommand TestCommand { get; }
		public ICommand DeleteCommand { get; }

		private void ExecuteConnect()
		{
			if (!_isConnected)
			{
				_isConnected = _serialPortService.Connect(SelectedComPort, 9600);
				if (_isConnected)
				{
					ConnectButtonText = "Disconnect";
					AppendToDisplay($"Connected to {SelectedComPort} at 9600 baud");
				}
				else
				{
					AppendToDisplay($"Error connecting to {SelectedComPort}");
				}
			}
			else
			{
				_serialPortService.Disconnect();
				_isConnected = false;
				ConnectButtonText = "Connect";
				AppendToDisplay("Disconnected");
			}
			OnPropertyChanged(nameof(IsNotConnected));
		}
		private void ExecuteSend()
		{
			if (!string.IsNullOrEmpty(SelectedCommand))
			{
				SendData(SelectedCommand);
			}
		}
		private void ExecuteSend(String command)
		{
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}
		private void ExecuteVersion()
		{
			String command = Settings.Default.CableTesterVersion;
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}
		private void ExecuteTest()
		{
			String command = Settings.Default.CableTesterTest;
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}
		private void ExecuteLearn()
		{
			String command = Settings.Default.CableTesterLearn;
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}
		private void ExecuteSave()
		{
			String command = Settings.Default.CableTesterSave;
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}
		private void ExecuteDelete()
		{
			String command = Settings.Default.CableTesterDelete;
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}
		private void ExecuteShow()
		{
			String command = Settings.Default.CableTesterShow;
			if (!string.IsNullOrEmpty(command))
			{
				SendData(command);
			}
		}


		private void SendData(string data)
		{
			if (_isConnected)
			{
				try
				{
					_serialPortService.WriteData(data);
					AppendToDisplay($"Sent: {data}");
				}
				catch (Exception ex)
				{
					AppendToDisplay($"Error sending data: {ex.Message}");
				}
			}
			else
			{
				AppendToDisplay("Cannot send data: Not connected");
			}
		}
		#endregion

		private void SerialPortService_DataReceived(object? sender, string e)
		{
			Application.Current.Dispatcher.Invoke(() => AppendToDisplay(e, true));
		}
		private void AppendToDisplay(string message, bool isReceived = false)
		{
			if (isReceived)
			{
				_receivedDataBuffer += message;
				while (_receivedDataBuffer.Contains(Environment.NewLine))
				{
					int newlineIndex = _receivedDataBuffer.IndexOf(Environment.NewLine);
					string line = _receivedDataBuffer.Substring(0, newlineIndex);
					DisplayText += $"{DateTime.Now:HH:mm:ss} - Received: {line}{Environment.NewLine}";
					_receivedDataBuffer = _receivedDataBuffer.Substring(newlineIndex + Environment.NewLine.Length);
				}
			}
			else
			{
				DisplayText += $"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}";
			}
			OnPropertyChanged(nameof(DisplayText));
		}




		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		public void Dispose()
		{
			_refreshTimer?.Stop();
			_refreshTimer?.Dispose();
			_serialPortService.Disconnect();
			_serialPortService.DataReceived -= SerialPortService_DataReceived;
		}
	}

}