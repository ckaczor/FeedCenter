using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChrisKaczor.Wpf.Application;
using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Data;
using FeedCenter.Properties;
using Serilog;

namespace FeedCenter
{
    public partial class MainWindow : IDisposable
    {
        private Category _currentCategory;
        private Feed _currentFeed;
        private FeedCenterEntities _database;
        private int _feedIndex;
        private IEnumerable<Feed> _feedList;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            _mainTimer?.Dispose();
            _feedReadWorker?.Dispose();

            GC.SuppressFinalize(this);
        }

        protected override async void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            await SingleInstance.Stop();
        }

        public async void Initialize()
        {
            // Setup the update handler
            InitializeUpdate();

            // Show the notification icon
            NotificationIcon.Initialize(this);

            // Load window settings
            LoadWindowSettings();

            // Set the foreground color to something that can be seen
            LinkTextList.Foreground = (System.Drawing.SystemColors.Desktop.GetBrightness() < 0.5)
                ? Brushes.White
                : Brushes.Black;
            HeaderLabel.Foreground = LinkTextList.Foreground;

            // Create the background worker that does the actual reading
            _feedReadWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _feedReadWorker.DoWork += HandleFeedReadWorkerStart;
            _feedReadWorker.ProgressChanged += HandleFeedReadWorkerProgressChanged;
            _feedReadWorker.RunWorkerCompleted += HandleFeedReadWorkerCompleted;

            // Setup the database
            _database = Database.Entities;

            // Initialize the single instance listener
            SingleInstance.MessageReceived += SingleInstance_MessageReceived;
            await SingleInstance.StartAsync(App.Name);

            // Handle any command line we were started with
            HandleCommandLine(Environment.CommandLine);

            // Create a timer to keep track of things we need to do
            InitializeTimer();

            // Initialize the feed display
            InitializeDisplay();

            // Check for update
            if (Settings.Default.CheckVersionAtStartup)
                await UpdateCheck.CheckForUpdate();

            // Show the link if updates are available
            if (UpdateCheck.UpdateAvailable)
                NewVersionLink.Visibility = Visibility.Visible;

            Log.Logger.Information("MainForm creation finished");
        }

        private void SingleInstance_MessageReceived(object sender, string commandLine)
        {
            HandleCommandLine(commandLine);
        }

        #region Setting events

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Make sure we're on the right thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler<PropertyChangedEventArgs>(HandlePropertyChanged), sender, e);
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(Settings.Default.MultipleLineDisplay):
                    // Update the current feed
                    DisplayFeed();
                    break;
                case nameof(Settings.Default.WindowLocked):
                    // Update the window for the new window lock value
                    HandleWindowLockState();
                    break;
                case nameof(Settings.Default.ToolbarLocation):
                    // Update the window for the toolbar location
                    switch (Settings.Default.ToolbarLocation)
                    {
                        case Dock.Top:
                            NameBasedGrid.NameBasedGrid.SetRow(NavigationToolbarTray, "TopToolbarRow");

                            break;
                        case Dock.Bottom:
                            NameBasedGrid.NameBasedGrid.SetRow(NavigationToolbarTray, "BottomToolbarRow");

                            break;
                        case Dock.Left:
                        case Dock.Right:
                        default:
                            throw new NotSupportedException();
                    }

                    break;
            }
        }

        #endregion

        #region Database helpers

        private void ResetDatabase()
        {
            // Get the ID of the current feed
            var currentId = _currentFeed?.Id ?? Guid.Empty;

            // Create a new database object
            _database.Refresh();

            _feedList = _currentCategory == null
                ? _database.Feeds.ToList()
                : _database.Feeds.Where(feed => feed.Category.Id == _currentCategory.Id).ToList();

            UpdateToolbarButtonState();

            // Get a list of feeds ordered by name
            var feedList = _feedList.OrderBy(f => f.Name).ToList();

            // First try to find the current feed by ID to see if it is still there
            var newIndex = feedList.FindIndex(f => f.Id == currentId);

            if (newIndex == -1)
            {
                // The current feed isn't there anymore so see if we can find a feed at the old index
                if (feedList.ElementAtOrDefault(_feedIndex) != null)
                    newIndex = _feedIndex;

                // If there is no feed at the old location then give up and go back to the start
                if (newIndex == -1 && feedList.Count > 0)
                    newIndex = 0;
            }

            // Set the current index to the new index
            _feedIndex = newIndex;

            // Re-get the current feed
            _currentFeed = (_feedIndex == -1
                ? null
                : _feedList.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex));
        }

        #endregion

        #region Feed display

        private void UpdateToolbarButtonState()
        {
            // Cache the feed count to save (a little) time
            var feedCount = _feedList?.Count() ?? 0;

            // Set button states
            PreviousToolbarButton.IsEnabled = feedCount > 1;
            NextToolbarButton.IsEnabled = feedCount > 1;
            RefreshToolbarButton.IsEnabled = feedCount > 0;
            FeedButton.IsEnabled = feedCount > 0;
            OpenAllToolbarButton.IsEnabled = feedCount > 0;
            MarkReadToolbarButton.IsEnabled = feedCount > 0;
            FeedLabel.Visibility = feedCount == 0 ? Visibility.Hidden : Visibility.Visible;
            FeedButton.Visibility = feedCount > 1 ? Visibility.Hidden : Visibility.Visible;
            CategoryGrid.Visibility = _database.Categories.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitializeDisplay()
        {
            // Get the last category (defaulting to none)
            _currentCategory =
                _database.Categories.FirstOrDefault(category =>
                    category.Id.ToString() == Settings.Default.LastCategoryID);
            DisplayCategory();

            // Get the current feed list to match the category
            _feedList = _currentCategory == null
                ? _database.Feeds
                : _database.Feeds.Where(feed => feed.Category.Id == _currentCategory.Id);

            UpdateToolbarButtonState();

            // Clear the link list
            LinkTextList.Items.Clear();

            // Refresh the feed index
            _feedIndex = -1;

            // Start the timer
            StartTimer();

            // Don't go further if we have no feeds
            if (!_feedList.Any())
                return;

            // Get the first feed
            NextFeed();
        }

        private void NextFeed()
        {
            var feedCount = _feedList.Count();

            if (feedCount == 0)
                return;

            if (Settings.Default.DisplayEmptyFeeds)
            {
                // Increment the index and adjust if we've gone around the end
                _feedIndex = (_feedIndex + 1) % feedCount;

                // Get the feed
                _currentFeed = _feedList.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);
            }
            else
            {
                // Keep track if we found something
                var found = false;

                // Remember our starting position
                var startIndex = (_feedIndex == -1 ? 0 : _feedIndex);

                // Increment the index and adjust if we've gone around the end
                _feedIndex = (_feedIndex + 1) % feedCount;

                // Loop until we come back to the start index
                do
                {
                    // Get the feed
                    _currentFeed = _feedList.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);

                    // If the current feed has unread items then we can display it
                    if (_currentFeed.Items.Any(item => !item.BeenRead))
                    {
                        found = true;
                        break;
                    }

                    // Increment the index and adjust if we've gone around the end
                    _feedIndex = (_feedIndex + 1) % feedCount;
                } while (_feedIndex != startIndex);

                // If nothing was found then clear the current feed
                if (!found)
                {
                    _feedIndex = -1;
                    _currentFeed = null;
                }
            }

            // Update the feed timestamp
            _lastFeedDisplay = DateTime.Now;

            // Update the display
            DisplayFeed();
        }

        private void PreviousFeed()
        {
            var feedCount = _feedList.Count();

            if (feedCount == 0)
                return;

            if (Settings.Default.DisplayEmptyFeeds)
            {
                // Decrement the feed index
                _feedIndex--;

                // If we've gone below the start of the list then reset to the end
                if (_feedIndex < 0)
                    _feedIndex = feedCount - 1;

                // Get the feed
                _currentFeed = _feedList.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);
            }
            else
            {
                // Keep track if we found something
                var found = false;

                // Remember our starting position
                var startIndex = (_feedIndex == -1 ? 0 : _feedIndex);

                // Decrement the feed index
                _feedIndex--;

                // If we've gone below the start of the list then reset to the end
                if (_feedIndex < 0)
                    _feedIndex = feedCount - 1;

                // Loop until we come back to the start index
                do
                {
                    // Get the feed
                    _currentFeed = _feedList.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);

                    // If the current feed has unread items then we can display it
                    if (_currentFeed.Items.Any(item => !item.BeenRead))
                    {
                        found = true;
                        break;
                    }

                    // Decrement the feed index
                    _feedIndex--;

                    // If we've gone below the start of the list then reset to the end
                    if (_feedIndex < 0)
                        _feedIndex = feedCount - 1;
                } while (_feedIndex != startIndex);

                // If nothing was found then clear the current feed
                if (!found)
                {
                    _feedIndex = -1;
                    _currentFeed = null;
                }
            }

            // Update the feed timestamp
            _lastFeedDisplay = DateTime.Now;

            // Update the display
            DisplayFeed();
        }

        private void UpdateOpenAllButton()
        {
            var multipleOpenAction = _currentFeed.MultipleOpenAction;

            switch (multipleOpenAction)
            {
                case MultipleOpenAction.IndividualPages:
                    OpenAllToolbarButton.ToolTip = Properties.Resources.openAllMultipleToolbarButton;
                    break;
                case MultipleOpenAction.SinglePage:
                    OpenAllToolbarButton.ToolTip = Properties.Resources.openAllSingleToolbarButton;
                    break;
            }
        }

        private void DisplayFeed()
        {
            // Just clear the display if we have no feed
            if (_currentFeed == null)
            {
                FeedLabel.Text = string.Empty;
                FeedButton.Visibility = Visibility.Hidden;
                LinkTextList.Items.Clear();

                return;
            }

            // Set the header to the feed title
            FeedLabel.Text = (_currentFeed.Name.Length > 0 ? _currentFeed.Name : _currentFeed.Title);
            FeedButton.Visibility = _feedList.Count() > 1 ? Visibility.Visible : Visibility.Hidden;

            // Clear the current list
            LinkTextList.Items.Clear();

            // Sort the items by sequence
            var sortedItems = _currentFeed.Items.Where(item => !item.BeenRead).OrderBy(item => item.Sequence);

            // Loop over all items in the current feed
            foreach (var feedItem in sortedItems)
            {
                // Add the list item
                LinkTextList.Items.Add(feedItem);
            }

            UpdateOpenAllButton();
        }

        private void MarkAllItemsAsRead()
        {
            // Loop over all items and mark them as read
            _database.SaveChanges(() =>
            {
                foreach (FeedItem feedItem in LinkTextList.Items)
                    feedItem.BeenRead = true;
            });

            // Clear the list
            LinkTextList.Items.Clear();
        }

        #endregion
    }
}