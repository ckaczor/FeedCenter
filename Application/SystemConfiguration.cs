using Common.Wpf.Extensions;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using FeedCenter.Properties;

namespace FeedCenter
{
    public static class SystemConfiguration
    {
        public static void SetDefaultFeedReader()
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

        public static bool UseDebugPath
        {
            get
            {
                return Environment.CommandLine.IndexOf("/debugPath", StringComparison.InvariantCultureIgnoreCase) != -1;
            }
        }

        public static string DataDirectory
        {
            get
            {
                return UseDebugPath ? Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) : UserSettingsPath;
            }
        }

        public static string UserSettingsPath
        {
            get
            {
                // If we're running in debug mode then use a local path for the database and logs
                if (UseDebugPath)
                    return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                // Get the path to the local application data directory
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Properties.Resources.ApplicationName);

                // Make sure it exists - create it if needed
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }
    }
}
