using FeedCenter.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FeedCenter.Update
{
    public static class UpdateCheck
    {
        public static VersionFile VersionFile { get; private set; }
        public static string LocalInstallFile { get; private set; }

        public static bool UpdateAvailable { get; private set; }

        public static Version CurrentVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static bool CheckForUpdate()
        {
            VersionFile = VersionFile.Load();

            if (VersionFile == null)
                return false;

            var serverVersion = VersionFile.Version;
            var localVersion = CurrentVersion;

            UpdateAvailable = serverVersion > localVersion;

            return true;
        }

        internal static async Task<bool> DownloadUpdate()
        {
            if (VersionFile == null)
                return false;

            var remoteFile = Settings.Default.VersionLocation + VersionFile.InstallFile;

            LocalInstallFile = Path.Combine(Path.GetTempPath(), VersionFile.InstallFile);

            var webClient = new WebClient();

            await webClient.DownloadFileTaskAsync(new Uri(remoteFile), LocalInstallFile);

            return true;
        }

        internal static bool InstallUpdate()
        {
            if (VersionFile == null)
                return false;

            Process.Start(LocalInstallFile, "/passive");

            return true;
        }
    }
}
