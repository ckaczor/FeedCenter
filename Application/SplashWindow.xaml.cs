using Common.Update;
using FeedCenter.Data;
using FeedCenter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace FeedCenter
{
    public partial class SplashWindow
    {
        #region Progress step

        private class ProgressStep
        {
            public delegate bool ProgressCallback();

            public readonly string Key;
            public readonly string Caption;
            public readonly ProgressCallback Callback;

            public ProgressStep(string key, string caption, ProgressCallback callback)
            {
                Key = key;
                Caption = caption;
                Callback = callback;
            }
        }

        #endregion

        #region Member variables

        private readonly List<ProgressStep> _progressSteps = new List<ProgressStep>();
        private readonly Dispatcher _dispatcher;
        private readonly BackgroundWorker _backgroundWorker;

        #endregion

        #region Constructor

        public SplashWindow()
        {
            InitializeComponent();

            // Store the dispatcher - the background worker has trouble getting the right thread when called from Main
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Get the version to display
            string version = UpdateCheck.CurrentVersion.ToString();

            // Show the version
            lblVersion.Content = string.Format(Properties.Resources.Version, version);

            // Set the starting caption
            lblStatus.Content = Properties.Resources.SplashStarting;

            // Build the progress steps
            LoadProgressSteps();

            // Set the progress bar to the number of steps
            progressBar.Maximum = _progressSteps.Count;

            // Create the worker with progress and cancel
            _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            // Setup the events
            _backgroundWorker.DoWork += HandleBackgroundWorkerDoWork;
            _backgroundWorker.ProgressChanged += HandleBackgroundWorkerProgressChanged;
            _backgroundWorker.RunWorkerCompleted += HandleBackgroundWorkerCompleted;
        }

        #endregion

        #region Form overrides

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Start the worker
            _backgroundWorker.RunWorkerAsync();
        }

        #endregion

        #region Background worker

        private void HandleBackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(new EventHandler<ProgressChangedEventArgs>(HandleBackgroundWorkerProgressChanged), sender, e);
                return;
            }

            // Update the progress bar
            progressBar.Value += e.ProgressPercentage;

            // Get the message
            var message = (string) e.UserState;

            // Update the status label if one was supplied
            if (!string.IsNullOrEmpty(message))
                lblStatus.Content = message;
        }

        private void HandleBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            // Wait just a little bit to make sure the window is up
            Thread.Sleep(100);

            // Initialize the skip key
            string skipKey = string.Empty;

            // Loop over all progress steps and execute
            foreach (ProgressStep progressStep in _progressSteps)
            {
                if (progressStep.Key == skipKey)
                {
                    // Update progress with an empty step
                    UpdateProgress(_backgroundWorker, string.Empty);
                }
                else
                {
                    // Update progress
                    UpdateProgress(_backgroundWorker, progressStep.Caption);

                    // Execute the step and get the result
                    bool result = progressStep.Callback();

                    // If the step indicated a skip then set the skip key, otherwise clear it
                    skipKey = (result ? string.Empty : progressStep.Key);
                }

                // Stop if cancelled
                if (_backgroundWorker.CancellationPending)
                    return;
            }
        }

        private static void UpdateProgress(BackgroundWorker worker, string progressMessage)
        {
            // Update the worker
            worker.ReportProgress(1, progressMessage);

            // Sleep a bit if we actually updated
            if (!string.IsNullOrEmpty(progressMessage))
                Thread.Sleep(Settings.Default.ProgressSleepInterval);
        }

        private void HandleBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(new EventHandler<RunWorkerCompletedEventArgs>(HandleBackgroundWorkerCompleted), sender, e);
                return;
            }

            // Move the progress bar to the max just in case
            progressBar.Value = progressBar.Maximum;

            // Close the window
            Close();
        }

        #endregion

        #region Progress steps

        private static class ProgressKey
        {
            public const string Update = "Update";
            public const string DatabaseCreate = "CreateDatabase";
            public const string DatabaseUpdate = "UpdateDatabase";
            public const string DatabaseMaintenance = "MaintainDatabase";
        }

        private void LoadProgressSteps()
        {
            // Load the progress steps
            _progressSteps.Add(new ProgressStep(ProgressKey.DatabaseCreate, Properties.Resources.SplashCheckingForDatabase, CheckDatabase));
            _progressSteps.Add(new ProgressStep(ProgressKey.DatabaseCreate, Properties.Resources.SplashCreatingDatabase, CreateDatabase));

            _progressSteps.Add(new ProgressStep(ProgressKey.DatabaseUpdate, Properties.Resources.SplashUpdatingDatabase, UpdateDatabase));

            _progressSteps.Add(new ProgressStep(ProgressKey.DatabaseMaintenance, Properties.Resources.SplashMaintainingDatabase, MaintainDatabase));

            _progressSteps.Add(new ProgressStep(ProgressKey.Update, Properties.Resources.SplashCheckingForUpdate, CheckUpdate));
        }

        private static bool CheckUpdate()
        {
            // If the user does not want to check version at startup then we're done
            if (!Settings.Default.CheckVersionAtStartup)
                return false;

            // Return if the check worked and an update is available
            return UpdateCheck.CheckForUpdate(Settings.Default.VersionLocation, Settings.Default.VersionFile) && UpdateCheck.UpdateAvailable;
        }

        private static bool CheckDatabase()
        {
            // Get the data directory
            string path = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();

            // Set the path
            Database.DatabasePath = System.IO.Path.Combine(path, Settings.Default.DatabaseFile);

            // If the database exists then we're done
            return !Database.DatabaseExists;
        }

        private static bool CreateDatabase()
        {
            // Create the database
            Database.CreateDatabase();

            return true;
        }

        private static bool UpdateDatabase()
        {
            // Update the database
            Database.UpdateDatabase();

            return true;
        }

        private static bool MaintainDatabase()
        {
            // Maintain the database
            Database.MaintainDatabase();

            return true;
        }

        #endregion
    }
}
