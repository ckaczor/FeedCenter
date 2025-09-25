using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Data;
using FeedCenter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace FeedCenter;

public partial class SplashWindow : IDisposable
{
    private class ProgressStep(string key, string caption, ProgressStep.ProgressCallback callback)
    {
        public delegate bool ProgressCallback();

        public readonly string Key = key;
        public readonly string Caption = caption;
        public readonly ProgressCallback Callback = callback;
    }

    private readonly List<ProgressStep> _progressSteps = new();
    private readonly Dispatcher _dispatcher;
    private readonly BackgroundWorker _backgroundWorker;

    public SplashWindow()
    {
        InitializeComponent();

        // Store the dispatcher - the background worker has trouble getting the right thread when called from Main
        _dispatcher = Dispatcher.CurrentDispatcher;

        VersionLabel.Content = string.Format(Properties.Resources.Version, UpdateCheck.LocalVersion.ToString());

        StatusLabel.Content = Properties.Resources.SplashStarting;

        LoadProgressSteps();

        ProgressBar.Maximum = _progressSteps.Count;

        _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

        _backgroundWorker.DoWork += HandleBackgroundWorkerDoWork;
        _backgroundWorker.ProgressChanged += HandleBackgroundWorkerProgressChanged;
        _backgroundWorker.RunWorkerCompleted += HandleBackgroundWorkerCompleted;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        _backgroundWorker.RunWorkerAsync();
    }

    private void HandleBackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.Invoke(new EventHandler<ProgressChangedEventArgs>(HandleBackgroundWorkerProgressChanged), sender, e);
            return;
        }

        ProgressBar.Value += e.ProgressPercentage;

        // Get the message
        var message = (string) e.UserState;

        // Update the status label if one was supplied
        if (!string.IsNullOrEmpty(message))
            StatusLabel.Content = message;
    }

    private void HandleBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
    {
        // Wait just a little bit to make sure the window is up
        Thread.Sleep(100);

        // Initialize the skip key
        var skipKey = string.Empty;

        // Loop over all progress steps and execute
        foreach (var progressStep in _progressSteps)
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
                var result = progressStep.Callback();

                // If the step indicated a skip then set the skip key, otherwise clear it
                skipKey = result ? string.Empty : progressStep.Key;
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
        ProgressBar.Value = ProgressBar.Maximum;

        Close();
    }

    private static class ProgressKey
    {
        public const string ManageLegacyDatabase = "ManageLegacyDatabase";
        public const string ManageDatabase = "ManageDatabase";
    }

    private void LoadProgressSteps()
    {
        _progressSteps.Add(new ProgressStep(ProgressKey.ManageLegacyDatabase, Properties.Resources.SplashCheckingForLegacyDatabase, CheckDatabase));
        _progressSteps.Add(new ProgressStep(ProgressKey.ManageLegacyDatabase, Properties.Resources.SplashUpdatingLegacyDatabase, UpdateDatabase));
        _progressSteps.Add(new ProgressStep(ProgressKey.ManageLegacyDatabase, Properties.Resources.SplashMaintainingLegacyDatabase, MaintainDatabase));
        _progressSteps.Add(new ProgressStep(ProgressKey.ManageLegacyDatabase, Properties.Resources.SplashMigratingLegacyDatabase, MigrateDatabase));

        _progressSteps.Add(new ProgressStep(ProgressKey.ManageDatabase, Properties.Resources.SplashLoadingDatabase, LoadDatabase));
    }

    private static bool CheckDatabase()
    {
        return LegacyDatabase.Exists;
    }

    private static bool UpdateDatabase()
    {
        LegacyDatabase.UpdateDatabase();

        return true;
    }

    private static bool MaintainDatabase()
    {
        LegacyDatabase.MaintainDatabase();

        return true;
    }

    private static bool MigrateDatabase()
    {
        LegacyDatabase.MigrateDatabase();

        return true;
    }

    private bool LoadDatabase()
    {
        _dispatcher.Invoke(() => Settings.Default.Reload());

        return true;
    }

    public void Dispose()
    {
        _backgroundWorker?.Dispose();

        GC.SuppressFinalize(this);
    }
}