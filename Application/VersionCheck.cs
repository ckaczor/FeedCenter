using Common.Debug;
using Common.Update;
using FeedCenter.Properties;
using System;
using System.ComponentModel;
using System.Windows;
using Application = System.Windows.Forms.Application;

namespace FeedCenter
{
    internal static class VersionCheck
    {
        public static async void DisplayUpdateInformation(bool showIfCurrent)
        {
            UpdateCheck.CheckForUpdate(Settings.Default.VersionLocation, Settings.Default.VersionFile);

            // Check for an update
            if (UpdateCheck.UpdateAvailable)
            {
                // Load the version string from the server
                Version serverVersion = UpdateCheck.VersionInfo.Version;

                // Format the check title
                string updateCheckTitle = string.Format(Resources.UpdateCheckTitle, Resources.ApplicationDisplayName);

                // Format the message
                string updateCheckMessage = string.Format(Resources.UpdateCheckNewVersion, Resources.ApplicationDisplayName, serverVersion);

                // Ask the user to update
                if (MessageBox.Show(updateCheckMessage, updateCheckTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                // Get the update
                await UpdateCheck.DownloadUpdate();

                // Install the update
                UpdateCheck.InstallUpdate();

                // Set to restart
                ((App) System.Windows.Application.Current).Restart = true;

                // Restart the application
                System.Windows.Application.Current.Shutdown();
            }
            else if (showIfCurrent)
            {
                // Format the check title
                string updateCheckTitle = string.Format(Resources.UpdateCheckTitle, Resources.ApplicationDisplayName);

                // Format the message
                string updateCheckMessage = string.Format(Resources.UpdateCheckCurrent, Resources.ApplicationDisplayName);

                MessageBox.Show(updateCheckMessage, updateCheckTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #region Background checking

        private static BackgroundWorker _backgroundWorker;

        public static void DisplayUpdateInformationAsync()
        {
            // Do nothing if we already have a worker
            if (_backgroundWorker != null)
                return;

            // Create a new worker
            _backgroundWorker = new BackgroundWorker();

            // Setup worker events
            _backgroundWorker.DoWork += HandleBackgroundWorkerDoWork;
            _backgroundWorker.RunWorkerCompleted += HandleBackgroundWorkerCompleted;

            // Run the worker
            _backgroundWorker.RunWorkerAsync();

            // Wait for the worker
            while (_backgroundWorker.IsBusy)
                Application.DoEvents();

            // Clear out the worker
            _backgroundWorker.Dispose();
            _backgroundWorker = null;
        }

        private static void HandleBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = null;

            try
            {
                UpdateCheck.CheckForUpdate(Settings.Default.VersionLocation, Settings.Default.VersionFile);

                // Get the update information and set it into the result
                e.Result = UpdateCheck.UpdateAvailable;
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);
            }
        }

        private static void HandleBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Display any update info
            DisplayUpdateInformation(false);
        }

        #endregion
    }
}
