using Common.Debug;
using Common.Helpers;
using Common.IO;
using Common.Settings;
using Common.Wpf.Extensions;
using FeedCenter.Properties;
using System;
using System.Diagnostics;
using System.Globalization;

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

        [STAThread]
        public static void Main()
        {
            // Create and initialize the app object
            var app = new App();
            app.InitializeComponent();

            // Create an isolation handle to see if we are already running
            var isolationHandle = ApplicationIsolation.GetIsolationHandle(FeedCenter.Properties.Resources.ApplicationName);

            // If there is another copy then pass it the command line and exit
            if (isolationHandle == null)
            {
                InterprocessMessageSender.SendMessage(FeedCenter.Properties.Resources.ApplicationName, Environment.CommandLine);
                return;
            }

            // Use the handle over the lifetime of the application
            using (isolationHandle)
            {
                // Set the data directory based on debug or not
                AppDomain.CurrentDomain.SetData("DataDirectory", SystemConfiguration.DataDirectory);

                // Get the generic provider
                var genericProvider = (GenericSettingsProvider) Settings.Default.Providers[typeof(GenericSettingsProvider).Name];

                if (genericProvider == null)
                    return;

                // Set the callbacks into the provider
                genericProvider.OpenDataStore = SettingsStore.OpenDataStore;
                genericProvider.CloseDataStore = SettingsStore.CloseDataStore;
                genericProvider.GetSettingValue = SettingsStore.GetSettingValue;
                genericProvider.SetSettingValue = SettingsStore.SetSettingValue;
                genericProvider.DeleteSettingsForVersion = SettingsStore.DeleteSettingsForVersion;
                genericProvider.GetVersionList = SettingsStore.GetVersionList;
                genericProvider.DeleteOldVersionsOnUpgrade = !IsDebugBuild;

                // Initialize the tracer with the current process ID
                Tracer.Initialize(SystemConfiguration.UserSettingsPath, FeedCenter.Properties.Resources.ApplicationName, Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), false);

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

                // Set whether we should auto-start
                Current.SetStartWithWindows(Settings.Default.StartWithWindows);

                // Initialize the window
                mainWindow.Initialize();

                // Run the app
                app.Run(mainWindow);

                // Terminate the tracer
                Tracer.Dispose();
            }
        }

        private static void HandleCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Tracer.WriteException((Exception) e.ExceptionObject);
            Tracer.Flush();
        }

        private static void HandleCurrentDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Tracer.WriteException(e.Exception);
            Tracer.Flush();
        }
    }
}
