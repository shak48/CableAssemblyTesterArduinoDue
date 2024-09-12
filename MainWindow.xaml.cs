using System;
using System.Windows;
using System.Reflection;

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

            // Optionally, set DataContext if needed for the rest of the application
            DataContext = this;
        }
    }
}