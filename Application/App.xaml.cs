using Common.Debug;
using Common.Helpers;
using Common.IO;
using Common.Settings;
using Common.Wpf.Extensions;
using FeedCenter.Properties;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace FeedCenter
{
    public partial class App 
    {
        #region Debug properties

        private static bool _useDebugPath;

        #endregion

        #region Main function

        [STAThread]
        public static void Main()
        {
            _useDebugPath = Environment.CommandLine.IndexOf("/debugPath", StringComparison.InvariantCultureIgnoreCase) != -1;

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
                AppDomain.CurrentDomain.SetData("DataDirectory",
                                                _useDebugPath
                                                    ? Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                                                    : UserSettingsPath);

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
                genericProvider.DeleteOldVersionsOnUpgrade = false;

                // Initialize the tracer with the current process ID
                Tracer.Initialize(UserSettingsPath, FeedCenter.Properties.Resources.ApplicationName, Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), false);

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

                // Update the registry settings
                SetStartWithWindows(Settings.Default.StartWithWindows);
                SetDefaultFeedReader(Settings.Default.RegisterAsDefaultFeedReader);

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

        #endregion

        #region Helpers

        public static string UserSettingsPath
        {
            get
            {
                // If we're running in debug mode then use a local path for the database and logs
                if (_useDebugPath)
                    return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                // Get the path to the local application data directory
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    FeedCenter.Properties.Resources.ApplicationName);

                // Make sure it exists - create it if needed
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        public static void SetStartWithWindows(bool value)
        {
            // Get the application name
            var applicationName = FeedCenter.Properties.Resources.ApplicationDisplayName;

            // Get application details
            var publisherName = applicationName;
            var productName = applicationName;
            var allProgramsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var shortcutPath = Path.Combine(allProgramsPath, publisherName);

            // Build the auto start path
            shortcutPath = "\"" + Path.Combine(shortcutPath, productName) + ".appref-ms\"";

            // Set auto start
            Current.SetStartWithWindows(applicationName, shortcutPath, value);
        }

        public static void SetDefaultFeedReader(bool value)
        {
            // Get the location of the assembly
            var assemblyLocation = Assembly.GetEntryAssembly().Location;

            // Open the registry key (creating if needed)
            using (var registryKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\feed", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                if (registryKey == null)
                    return;

                // Write the handler settings
                registryKey.SetValue(string.Empty, "URL:Feed Handler");
                registryKey.SetValue("URL Protocol", string.Empty);

                // Open the icon subkey (creating if needed)
                using (var subKey = registryKey.CreateSubKey("DefaultIcon", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (subKey != null)
                    {
                        // Write the assembly location
                        subKey.SetValue(string.Empty, assemblyLocation);

                        // Close the subkey
                        subKey.Close();
                    }
                }

                // Open the subkey for the command (creating if needed)
                using (var subKey = registryKey.CreateSubKey("shell\\open\\command", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (subKey != null)
                    {
                        // Write the assembly location and parameter
                        subKey.SetValue(string.Empty, string.Format("\"{0}\" %1", assemblyLocation));

                        // Close the subkey
                        subKey.Close();
                    }
                }

                // Close the registry key
                registryKey.Close();
            }
        }

        #endregion
    }
}
