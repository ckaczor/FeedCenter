using FeedCenter.Properties;
using System;
using System.Timers;
using System.Windows.Threading;

namespace FeedCenter;

public partial class MainWindow
{
    private Timer _mainTimer;
    private DateTime _lastFeedRead;
    private DateTime _lastFeedDisplay;
    private Dispatcher _dispatcher;

    private void InitializeTimer()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;

        _mainTimer = new Timer { Interval = 1000 };
        _mainTimer.Elapsed += HandleMainTimerElapsed;
    }

    private void TerminateTimer()
    {
        StopTimer();

        _mainTimer?.Dispose();
        _mainTimer = null;
    }

    private void StartTimer()
    {
        _mainTimer?.Start();
    }

    private void StopTimer()
    {
        _mainTimer?.Stop();
    }

    private async void HandleMainTimerElapsed(object sender, EventArgs e)
    {
        try
        {
            await _dispatcher.Invoke(async () =>
            {
                // If the background worker is busy then don't do anything
                if (_reading)
                    return;

                // Stop the timer for now
                StopTimer();

                // Move to the next feed if the scroll interval has expired and the mouse isn't hovering
                if (LinkTextList.IsMouseOver)
                    _lastFeedDisplay = DateTime.Now;
                else if (DateTime.Now - _lastFeedDisplay >= Settings.Default.FeedScrollInterval)
                    NextFeed();

                // Check to see if we should try to read the feeds
                if (DateTime.Now - _lastFeedRead >= Settings.Default.FeedCheckInterval)
                    await ReadFeeds();

                // Get the timer going again
                StartTimer();
            });
        }
        catch (Exception exception)
        {
            HandleException(exception);
        }
    }
}