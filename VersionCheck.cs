using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Windows;

using Common.Debug;

using FeedCenter.Properties;
using Application = System.Windows.Forms.Application;

namespace FeedCenter
{
    internal static class VersionCheck
    {
        public static void DisplayUpdateInformation(bool showIfCurrent)
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
                return;

            UpdateCheckInfo updateCheckInfo = null;

            try
            {
                updateCheckInfo = ApplicationDeployment.CurrentDeployment.CheckForDetailedUpdate(false);
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);
            }

            DisplayUpdateInformation(updateCheckInfo, showIfCurrent);
        }

        public static void DisplayUpdateInformation(UpdateCheckInfo updateCheckInfo, bool showIfCurrent)
        {
            // If we didn't get any information then do nothing
            if (updateCheckInfo == null)
                return;

            // Check for an update
            if (updateCheckInfo.UpdateAvailable)
            {
                // Load the version string from the server
                Version serverVersion = updateCheckInfo.AvailableVersion;

                // Format the check title
                string updateCheckTitle = string.Format(Resources.UpdateCheckTitle, Resources.ApplicationDisplayName);

                // Format the message
                string updateCheckMessage = string.Format(Resources.UpdateCheckNewVersion, Resources.ApplicationDisplayName, serverVersion);

                // Ask the user to update
                if (MessageBox.Show(updateCheckMessage, updateCheckTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                // Get the update
                ApplicationDeployment.CurrentDeployment.Update();

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

        static void HandleBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            // If the application isn't installed then do nothing
            if (!ApplicationDeployment.IsNetworkDeployed)
                return;

            e.Result = null;

            try
            {
                // Get the update information and set it into the result
                e.Result = ApplicationDeployment.CurrentDeployment.CheckForDetailedUpdate(false);
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);
            }
        }

        private static void HandleBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Display any update info
            DisplayUpdateInformation(e.Result as UpdateCheckInfo, false);
        }

        #endregion
    }
}
