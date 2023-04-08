using System;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using ChrisKaczor.Wpf.Application;
using ChrisKaczor.GenericSettingsProvider;
using FeedCenter.Data;
using FeedCenter.Properties;
using Serilog;

namespace FeedCenter
{
    public partial class App
    {
        // ReSharper disable ConvertPropertyToExpressionBody
        private static bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
        // ReSharper restore ConvertPropertyToExpressionBody

        public static string Name => FeedCenter.Properties.Resources.ApplicationName;

        [STAThread]
        public static void Main()
        {
            // Create and initialize the app object
            var app = new App();
            app.InitializeComponent();

            // Create an single instance handle to see if we are already running
            var isolationHandle = SingleInstance.GetSingleInstanceHandleAsync(Name).Result;

            // If there is another copy then pass it the command line and exit
            if (isolationHandle == null)
                return;

            // Use the handle over the lifetime of the application
            using (isolationHandle)
            {
                // Set the path
                LegacyDatabase.DatabasePath = SystemConfiguration.DataDirectory;
                LegacyDatabase.DatabaseFile = Path.Combine(SystemConfiguration.DataDirectory,
                    Settings.Default.DatabaseFile_Legacy);

                Database.DatabasePath = SystemConfiguration.DataDirectory;
                Database.DatabaseFile = Path.Combine(SystemConfiguration.DataDirectory, Settings.Default.DatabaseFile);

                // Get the generic provider
                var genericProvider =
                    (GenericSettingsProvider) Settings.Default.Providers[nameof(GenericSettingsProvider)];

                if (genericProvider == null)
                    return;

                // Set the callbacks into the provider
                genericProvider.OpenDataStore = SettingsStore.OpenDataStore;
                genericProvider.GetSettingValue = SettingsStore.GetSettingValue;
                genericProvider.SetSettingValue = SettingsStore.SetSettingValue;

                Log.Logger = new LoggerConfiguration()
                    .Enrich.WithThreadId()
                    .WriteTo.Console()
                    .WriteTo.File(
                        Path.Join(SystemConfiguration.UserSettingsPath,
                            $"{FeedCenter.Properties.Resources.ApplicationName}_.txt"),
                        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5,
                        outputTemplate: "[{Timestamp:u} - {ThreadId} - {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                Log.Logger.Information("---");
                Log.Logger.Information("Application started");

                Log.Logger.Information("Command line arguments:");

                foreach (var arg in Environment.GetCommandLineArgs()
                             .Select((value, index) => (Value: value, Index: index)))
                    Log.Logger.Information("\tArg {0}: {1}", arg.Index, arg.Value);

                Current.DispatcherUnhandledException += HandleCurrentDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += HandleCurrentDomainUnhandledException;

                // Check if we need to upgrade settings from a previous version
                if (Settings.Default.FirstRun)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.FirstRun = false;
                    Settings.Default.Save();
                }

                // Create the main window before the splash otherwise WPF gets messed up
                var mainWindow = new MainWindow();

                // Show the splash window
                var splashWindow = new SplashWindow();
                splashWindow.ShowDialog();

                // Set whether we should auto-start (if not debugging)
                if (!IsDebugBuild)
                    Current.SetStartWithWindows(Settings.Default.StartWithWindows);

                // Initialize the window
                mainWindow.Initialize();

                // Run the app
                app.Run(mainWindow);
            }
        }

        private static void HandleCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Logger.Error((Exception) e.ExceptionObject, "Exception");
        }

        private static void HandleCurrentDispatcherUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Logger.Error(e.Exception, "Exception");
        }
    }
}