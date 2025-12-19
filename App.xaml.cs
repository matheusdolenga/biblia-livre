using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BibleApp.Services;
using BibleApp.ViewModels;
using System.IO;
using System;

namespace BibleApp
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            Log("App Constructor Started");
            AppDomain.CurrentDomain.UnhandledException += (s, e) => 
            {
                Log($"AppDomain Unhandled Exception: {e.ExceptionObject}");
                MessageBox.Show($"Critical Error: {e.ExceptionObject}");
            };
            
            DispatcherUnhandledException += (s, e) =>
            {
                Log($"Dispatcher Unhandled Exception: {e.Exception}");
                MessageBox.Show($"Dispatcher Error: {e.Exception.Message}");
                e.Handled = true;
            };

            Services = ConfigureServices();
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText("debug_log.txt", $"{DateTime.Now}: {message}\n");
            }
            catch { }
        }

        private static IServiceProvider ConfigureServices()
        {
            Log("Configuring Services");
            var services = new ServiceCollection();

            // Services
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "bible.db");
            Log($"DB Path: {dbPath}");
            
            services.AddSingleton<BibleService>(s => new BibleService(dbPath));
            services.AddSingleton<UserService>();
            services.AddSingleton<VerseOfDayService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Views (Optional if we use ViewFirst or ViewModelFirst)
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log("OnStartup Called");
            base.OnStartup(e);

            try
            {
                Log("Resolving MainWindow");
                var mainWindow = Services.GetRequiredService<MainWindow>();
                Log("Resolving MainViewModel");
                mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
                Log("Showing MainWindow");
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log($"Startup Exception: {ex}");
                MessageBox.Show($"Startup Error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
