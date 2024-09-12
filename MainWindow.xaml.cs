using System;
using System.Windows;
using System.Reflection;
using CableAssemblyTesterArduinoDue.Services;
using CableAssemblyTesterArduinoDue.ViewModels;

namespace CableAssemblyTesterArduinoDue
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            // Retrieve version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            // Set the Title with version info
            Title = $"Cable Assembly Tester - v{(version?.Major ?? 0)}.{(version?.Minor ?? 0)}.{(version?.Build ?? 0)}.{(version?.Revision ?? 0)}";
			// Create the ViewModel and set it as the DataContext for CableTesterView
			var serialPortService = new SerialPortService();
			var viewModel = new CableTesterViewModel(serialPortService);
			CableTesterViewControl.DataContext = viewModel;
		}
    }
}