using System;

namespace CableAssemblyTesterArduinoDue.Services
{
	public interface ISerialPortService
	{
		event EventHandler<string> DataReceived;
		bool IsConnected { get; }
		string[] GetAvailablePorts();
		bool Connect(string portName, int baudRate);
		void Disconnect();
		void WriteData(string data);
	}
}