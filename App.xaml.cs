using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using CableAssemblyTesterArduinoDue.Services;
using CableAssemblyTesterArduinoDue.ViewModels;
using System;

namespace CableAssemblyTesterArduinoDue
{
	public partial class App : Application
	{
		private ServiceProvider serviceProvider;

		public App()
		{
			ServiceCollection services = new ServiceCollection();
			ConfigureServices(services);
			serviceProvider = services.BuildServiceProvider();
		}

		private void ConfigureServices(ServiceCollection services)
		{
			services.AddSingleton<ISerialPortService, SerialPortService>();
			services.AddTransient<CableTesterViewModel>();
			services.AddSingleton<MainWindow>();
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show();
		}
	}
}