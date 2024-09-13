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
			set => SetProperty(ref _displayText, value);
		}

		public ICommand ConnectCommand { get; }
		public ICommand SendCommand { get; }
		public ICommand TestCommand { get; }
		public ICommand VersionCommand { get; }

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
			_refreshTimer = new Timer(5000) { AutoReset = true };
			_refreshTimer.Elapsed += (_, _) => RefreshAvailablePorts();
			_refreshTimer.Start();
			Initialize();
		}

		private void Initialize()
		{
			RefreshAvailablePorts();
			AppendToDisplay("CableTester initialized.");
		}


		private void LoadCommandsFromSettings()
		{
			try
			{
				AvailableCommands = new ObservableCollection<string>();

				// Get all properties from Settings

				string command = Settings.Default.FlukeQuery;

				if (!string.IsNullOrEmpty(command))
				{
					AvailableCommands.Add(command);
				}

				Console.WriteLine($"Loaded {AvailableCommands.Count} commands"); // Debug output


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

		private void ExecuteVersion() => SendData("version");

		private void ExecuteTest() => SendData("test");

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