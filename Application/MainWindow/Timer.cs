using FeedCenter.Properties;
using System;
using System.Windows.Forms;

namespace FeedCenter
{
    public partial class MainWindow
    {
        private Timer _mainTimer;
        private DateTime _lastFeedRead;
        private DateTime _lastFeedDisplay;

        private void InitializeTimer()
        {
            _mainTimer = new Timer { Interval = 1000 };
            _mainTimer.Tick += HandleMainTimerTick;
        }

        private void TerminateTimer()
        {
            StopTimer();

            _mainTimer.Dispose();
        }

        private void StartTimer()
        {
            _mainTimer.Start();
        }

        private void StopTimer()
        {
            _mainTimer.Stop();
        }

        private void HandleMainTimerTick(object sender, EventArgs e)
        {
            // If the background worker is busy then don't do anything
            if (_feedReadWorker.IsBusy)
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
                ReadFeeds();

            // Get the timer going again
            StartTimer();
        }
    }
}
