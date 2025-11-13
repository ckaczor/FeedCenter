using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Feeds;
using FeedCenter.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FeedCenter;

public partial class MainWindow
{
    private bool _reading;

    private void SetProgressMode(bool showProgress, int maximum)
    {
        // Refresh the progress bar if we need it
        if (showProgress)
        {
            FeedReadProgress.Value = 0;
            FeedReadProgress.Maximum = maximum + 2;
            FeedReadProgress.Visibility = Visibility.Visible;
        }
        else
        {
            FeedReadProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void ReadCurrentFeed(bool forceRead = false)
    {
        try
        {
            // Don't read if we're already working
            if (_reading)
                return;

            _reading = true;

            // Don't read if there is nothing to read
            if (!_database.Feeds.Any())
                return;

            var accountReadInput = new AccountReadInput(_database, _currentFeed.Id, forceRead, () => { });

            // Switch to progress mode
            SetProgressMode(true, await _currentFeed.Account.GetProgressSteps(_currentFeed.Account, accountReadInput));

            // Start reading
            await HandleFeedReadWorkerStart(forceRead, _currentFeed.Id);
        }
        catch (Exception exception)
        {
            HandleException(exception);
        }
    }

    private async Task ReadFeeds(bool forceRead = false)
    {
        // Don't read if we're already working
        if (_reading)
            return;

        _reading = true;

        // Don't read if there is nothing to read
        if (!_database.Accounts.Any())
            return;

        var accountReadInput = new AccountReadInput(_database, null, forceRead, () => { });

        // Calculate total progress steps
        var progressSteps = 0;

        foreach (var account in _database.Accounts)
        {
            progressSteps += await account.GetProgressSteps(account, accountReadInput);
        }

        // Switch to progress mode
        SetProgressMode(true, progressSteps);

        // Start reading 
        await HandleFeedReadWorkerStart(forceRead, null);
    }

    private void IncrementProgress()
    {
        Debug.Assert(FeedReadProgress.Value + 1 <= FeedReadProgress.Maximum);

        // Set progress
        FeedReadProgress.Value++;
    }

    private void CompleteRead()
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

        _reading = false;
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

    private async Task HandleFeedReadWorkerStart(bool forceRead, Guid? feedId)
    {
        try
        {
            // Create a new database instance for just this thread
            var database = new FeedCenterEntities();

            var accountsToRead = new List<Account>();

            // If we have a single feed then get the account for that feed
            if (feedId != null)
            {
                var feed = database.Feeds.FirstOrDefault(f => f.Id == feedId);

                if (feed != null)
                    accountsToRead.Add(feed.Account);
            }
            else
            {
                // Otherwise get all accounts
                accountsToRead.AddRange(database.Accounts);
            }

            // Loop over each account and read it
            foreach (var account in accountsToRead)
            {
                var accountReadInput = new AccountReadInput(database, feedId, forceRead, IncrementProgress);

                await account.Read(accountReadInput);
            }

            // Report progress
            IncrementProgress();

            // See if we're due for a version check
            if (UpdateCheck.LocalVersion.Major > 0 && DateTime.Now - Settings.Default.LastVersionCheck >= Settings.Default.VersionCheckInterval)
            {
                // Get the update information
                await UpdateCheck.CheckForUpdate(Settings.Default.IncludePrerelease);

                // Update the last check time
                Settings.Default.LastVersionCheck = DateTime.Now;
            }

            // Report progress
            IncrementProgress();

            // Sleep for a little bit so the user can see the update
            Thread.Sleep(Settings.Default.ProgressSleepInterval * 3);

            CompleteRead();
        }
        catch (Exception exception)
        {
            HandleException(exception);
        }
    }
}