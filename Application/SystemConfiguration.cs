using FeedCenter.Properties;
using System;
using System.IO;
using System.Reflection;

namespace FeedCenter;

public static class SystemConfiguration
{
    private static bool UseDebugPath => Environment.CommandLine.IndexOf("/debugPath", StringComparison.InvariantCultureIgnoreCase) != -1;

    public static string DataDirectory => UseDebugPath ? Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) : UserSettingsPath;

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
                Resources.ApplicationName);

            // Make sure it exists - create it if needed
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }
    }
}