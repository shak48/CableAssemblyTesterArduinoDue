using System;
using System.IO.Ports;

namespace CableAssemblyTesterArduinoDue.Services
{
	public class SerialPortService : ISerialPortService, IDisposable
	{
		private SerialPort? _serialPort;
		public event EventHandler<string>? DataReceived;

		public bool IsConnected => _serialPort?.IsOpen ?? false;

		public string[] GetAvailablePorts() => SerialPort.GetPortNames();

		public bool Connect(string portName, int baudRate)
		{
			try
			{
				_serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
				_serialPort.DataReceived += SerialPort_DataReceived;
				_serialPort.Open();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void Disconnect()
		{
			if (_serialPort != null)
			{
				_serialPort.DataReceived -= SerialPort_DataReceived;
				_serialPort.Close();
				_serialPort.Dispose();
				_serialPort = null;
			}
		}

		public void WriteData(string data)
		{
			if (IsConnected)
			{
				_serialPort?.WriteLine(data);
			}
		}

		private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (e.EventType == SerialData.Chars && _serialPort != null)
			{
				string indata = _serialPort.ReadExisting();
				DataReceived?.Invoke(this, indata);
			}
		}

		public void Dispose()
		{
			Disconnect();
		}

	}
}