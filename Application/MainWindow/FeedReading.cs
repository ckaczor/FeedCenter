using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace FeedCenter;

public partial class MainWindow
{
    private BackgroundWorker _feedReadWorker;

    private class FeedReadWorkerInput
    {
        public bool ForceRead { get; }
        public Guid? FeedId { get; }

        public FeedReadWorkerInput()
        {
        }

        public FeedReadWorkerInput(bool forceRead)
        {
            ForceRead = forceRead;
        }

        public FeedReadWorkerInput(bool forceRead, Guid? feedId)
        {
            ForceRead = forceRead;
            FeedId = feedId;
        }
    }

    private void SetProgressMode(bool value, int feedCount)
    {
        // Refresh the progress bar if we need it
        if (value)
        {
            FeedReadProgress.Value = 0;
            FeedReadProgress.Maximum = feedCount + 2;
            FeedReadProgress.Visibility = Visibility.Visible;
        }
        else
        {
            FeedReadProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void ReadCurrentFeed(bool forceRead = false)
    {
        // Don't read if we're already working
        if (_feedReadWorker.IsBusy)
            return;

        // Don't read if there is nothing to read
        if (!_database.Feeds.Any())
            return;

        // Switch to progress mode
        SetProgressMode(true, 1);

        // Create the input class
        var workerInput = new FeedReadWorkerInput(forceRead, _currentFeed.Id);

        // Start the worker
        _feedReadWorker.RunWorkerAsync(workerInput);
    }

    private void ReadFeeds(bool forceRead = false)
    {
        // Don't read if we're already working
        if (_feedReadWorker.IsBusy)
            return;

        // Don't read if there is nothing to read
        if (!_database.Feeds.Any())
            return;

        // Switch to progress mode
        SetProgressMode(true, _database.Feeds.Count);

        // Create the input class
        var workerInput = new FeedReadWorkerInput(forceRead);

        // Start the worker
        _feedReadWorker.RunWorkerAsync(workerInput);
    }

    private void HandleFeedReadWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        // Set progress
        FeedReadProgress.Value = e.ProgressPercentage;
    }

    private void HandleFeedReadWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        // Refresh the database to current settings
        ResetDatabase();

        // Save settings
        Settings.Default.Save();

        // Set the read timestamp
        _lastFeedRead = DateTime.Now;

        // Update the current feed
        DisplayFeed();

        // Switch to normal mode
        SetProgressMode(false, 0);

        // Check for update
        if (UpdateCheck.UpdateAvailable)
            NewVersionLink.Visibility = Visibility.Visible;

        UpdateErrorLink();
    }

    private void UpdateErrorLink()
    {
        var feedErrorCount = _database.Feeds.Count(f => f.LastReadResult != FeedReadResult.Success);

        // Set the visibility of the error link
        FeedErrorsLink.Visibility = feedErrorCount == 0 ? Visibility.Collapsed : Visibility.Visible;

        // Set the text to match the number of errors
        FeedErrorsLink.Text = feedErrorCount == 1
            ? Properties.Resources.FeedErrorLink
            : string.Format(Properties.Resources.FeedErrorsLink, feedErrorCount);
    }

    private static void HandleFeedReadWorkerStart(object sender, DoWorkEventArgs e)
    {
        // Create a new database instance for just this thread
        var database = new FeedCenterEntities();

        // Get the worker
        var worker = (BackgroundWorker) sender;

        // Get the input information
        var workerInput = (FeedReadWorkerInput) e.Argument ?? new FeedReadWorkerInput();

        // Setup for progress
        var currentProgress = 0;

        // Create the list of feeds to read
        var feedsToRead = new List<Feed>();

        // If we have a single feed then add it to the list - otherwise add them all
        if (workerInput.FeedId != null)
            feedsToRead.Add(database.Feeds.First(feed => feed.Id == workerInput.FeedId));
        else
            feedsToRead.AddRange(database.Feeds);

        // Loop over each feed and read it
        foreach (var feed in feedsToRead)
        {
            // Read the feed
            database.SaveChanges(() => feed.Read(workerInput.ForceRead));

            // Increment progress
            currentProgress += 1;

            // Report progress
            worker.ReportProgress(currentProgress);
        }

        // Increment progress
        currentProgress += 1;

        // Report progress
        worker.ReportProgress(currentProgress);

        // See if we're due for a version check
        if (DateTime.Now - Settings.Default.LastVersionCheck >= Settings.Default.VersionCheckInterval)
        {
            // Get the update information
            UpdateCheck.CheckForUpdate().Wait();

            // Update the last check time
            Settings.Default.LastVersionCheck = DateTime.Now;
        }

        // Increment progress
        currentProgress += 1;

        // Report progress
        worker.ReportProgress(currentProgress);

        // Sleep for a little bit so the user can see the update
        Thread.Sleep(Settings.Default.ProgressSleepInterval * 3);
    }
}