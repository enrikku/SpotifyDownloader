using System.IO;
using System.Reflection;
using System.Windows;

using log4net;
using log4net.Config;
using log4net.Repository;

namespace SpotifyPlayListDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ILoggerRepository repo = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repo, new FileInfo("log4net.config"));

            // Manejo global de excepciones (recomendado)
            this.DispatcherUnhandledException += (s, ex) =>
            {
                LogManager.GetLogger(typeof(App)).Fatal("Excepción UI no controlada", ex.Exception);
                ex.Handled = true; // Decide si quieres cerrar la app o no
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex2) =>
            {
                LogManager.GetLogger(typeof(App)).Fatal("Excepción no controlada (AppDomain)", ex2.ExceptionObject as Exception);
            };
            TaskScheduler.UnobservedTaskException += (s, ex3) =>
            {
                LogManager.GetLogger(typeof(App)).Fatal("Excepción Task no observada", ex3.Exception);
                ex3.SetObserved();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cierra correctamente los appenders
            log4net.LogManager.Shutdown();
            base.OnExit(e);
        }
    }
}