using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.IO;
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Linq;
using System.Threading;

using Common.Debug;
using Common.Helpers;
using Common.IO;
using Common.Internet;
using Common.Wpf.Extensions;
using FeedCenter.Options;
using FeedCenter.Properties;

namespace FeedCenter
{
    public partial class MainWindow
    {
        #region Member variables

        private int _feedIndex;
        private Feed _currentFeed;
        private DateTime _lastFeedDisplay;
        private DateTime _lastFeedRead;
        private System.Windows.Forms.Timer _mainTimer;
        private FeedCenterEntities _database;

        private InterprocessMessageListener _commandLineListener;
        private BackgroundWorker _feedReadWorker;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        private bool _activated;
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (_activated)
                return;

            _activated = true;

            // Load the lock state
            HandleWindowLockState();

            // Watch for size and location changes
            SizeChanged += HandleWindowSizeChanged;
            LocationChanged += HandleWindowLocationChanged;

            // Watch for setting changes
            Settings.Default.PropertyChanged += HandlePropertyChanged;
        }

        public void Initialize()
        {
            // Show the notification icon
            NotificationIcon.Initialize(this);

            // Load window settings
            LoadWindowSettings();

            // Set the foreground color to something that can be seen
            linkTextList.Foreground = (System.Drawing.SystemColors.Desktop.GetBrightness() < 0.5) ? Brushes.White : Brushes.Black;
            headerLabel.Foreground = linkTextList.Foreground;

            // Create the background worker that does the actual reading
            _feedReadWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _feedReadWorker.DoWork += HandleFeedReadWorkerStart;
            _feedReadWorker.ProgressChanged += HandleFeedReadWorkerProgressChanged;
            _feedReadWorker.RunWorkerCompleted += HandleFeedReadWorkerCompleted;

            // Setup the database
            _database = new FeedCenterEntities();

            // Initialize the command line listener
            _commandLineListener = new InterprocessMessageListener(Properties.Resources.ApplicationName);
            _commandLineListener.MessageReceived += HandleCommandLine;

            // Handle any command line we were started with
            HandleCommandLine(null, new InterprocessMessageListener.InterprocessMessageEventArgs(Environment.CommandLine));

            // Create a timer to keep track of things we need to do
            InitializeTimer();

            // Initialize the feed display
            InitializeFeed();

            Tracer.WriteLine("MainForm creation finished");
        }

        #endregion

        #region Window overrides

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // Ditch the worker
            if (_feedReadWorker != null)
            {
                _feedReadWorker.CancelAsync();
                _feedReadWorker.Dispose();
            }

            // Get rid of the timer
            TerminateTimer();

            // Save current window settings
            SaveWindowSettings();

            // Save settings
            Settings.Default.Save();

            // Save options
            _database.SaveChanges();

            // Get rid of the notification icon
            NotificationIcon.Dispose();
        }

        #endregion

        #region Setting events

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Make sure we're on the right thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new EventHandler<PropertyChangedEventArgs>(HandlePropertyChanged), sender, e);
                return;
            }

            if (e.PropertyName == Reflection.GetPropertyName(() => Settings.Default.MultipleLineDisplay))
            {
                // Update the current feed
                DisplayFeed();
            }
            else if (e.PropertyName == Reflection.GetPropertyName(() => Settings.Default.WindowLocked))
            {
                // Update the window for the new window lock value
                HandleWindowLockState();
            }
            else if (e.PropertyName == Reflection.GetPropertyName(() => Settings.Default.ToolbarLocation))
            {
                // Update the window for the toolbar location
                switch (Settings.Default.ToolbarLocation)
                {
                    case Dock.Top:
                        Grid.SetRow(navigationToolbarTray, mainGrid.GetRowIndex(topToolbarRow));

                        break;
                    case Dock.Bottom:
                        Grid.SetRow(navigationToolbarTray, mainGrid.GetRowIndex(bottomToolbarRow));
                        break;
                }
            }
        }

        #endregion

        #region Window methods

        private void LoadWindowSettings()
        {
            // Get the last window location
            Point windowLocation = Settings.Default.WindowLocation;

            // Set the window into position
            Left = windowLocation.X;
            Top = windowLocation.Y;

            // Get the last window size
            Size windowSize = Settings.Default.WindowSize;

            // Set the window into the previous size if it is valid
            if (!windowSize.Width.Equals(0) && !windowSize.Height.Equals(0))
            {
                Width = windowSize.Width;
                Height = windowSize.Height;
            }

            // Set the location of the navigation tray
            switch (Settings.Default.ToolbarLocation)
            {
                case Dock.Top:
                    Grid.SetRow(navigationToolbarTray, mainGrid.GetRowIndex(topToolbarRow));
                    break;
                case Dock.Bottom:
                    Grid.SetRow(navigationToolbarTray, mainGrid.GetRowIndex(bottomToolbarRow));
                    break;
            }

            // Load the lock state
            HandleWindowLockState();
        }

        private void SaveWindowSettings()
        {
            // Set the last window location
            Settings.Default.WindowLocation = new Point(Left, Top);

            // Set the last window size
            Settings.Default.WindowSize = new Size(Width, Height);

            // Save the dock on the navigation tray
            Settings.Default.ToolbarLocation = Grid.GetRow(navigationToolbarTray) == mainGrid.GetRowIndex(topToolbarRow) ? Dock.Top : Dock.Bottom;

            // Save settings
            Settings.Default.Save();
        }

        private void HandleWindowLockState()
        {
            // Set the resize mode for the window
            ResizeMode = Settings.Default.WindowLocked ? ResizeMode.NoResize : ResizeMode.CanResize;

            // Show or hide the border
            windowBorder.BorderBrush = Settings.Default.WindowLocked ? SystemColors.ActiveBorderBrush : Brushes.Transparent;

            // Update the borders
            UpdateBorder();
        }

        #endregion

        #region Header events

        private void HandleHeaderLabelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore if the window is locked
            if (Settings.Default.WindowLocked)
                return;

            // Start dragging
            DragMove();
        }

        private void HandleCloseButtonClick(object sender, RoutedEventArgs e)
        {
            // Close the window
            Close();
        }

        #endregion

        #region Timer handling

        private void InitializeTimer()
        {
            _mainTimer = new System.Windows.Forms.Timer { Interval = 1000 };
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
            if (linkTextList.IsMouseOver)
                _lastFeedDisplay = DateTime.Now;
            else if (DateTime.Now - _lastFeedDisplay >= Settings.Default.FeedScrollInterval)
                NextFeed();

            // Check to see if we should try to read the feeds
            if (DateTime.Now - _lastFeedRead >= Settings.Default.FeedCheckInterval)
                ReadFeeds();

            // Get the timer going again
            StartTimer();
        }

        #endregion

        #region Feed display

        private void InitializeFeed()
        {
            // Cache the feed count to save (a little) time
            int feedCount = _database.Feeds.Count();

            // Set button states
            previousToolbarButton.IsEnabled = (feedCount > 1);
            nextToolbarButton.IsEnabled = (feedCount > 1);
            refreshToolbarButton.IsEnabled = (feedCount > 0);
            feedLabel.Visibility = (feedCount == 0 ? Visibility.Hidden : Visibility.Visible);
            feedButton.Visibility = (feedCount > 1 ? Visibility.Hidden : Visibility.Visible);

            // Clear the link list
            linkTextList.Items.Clear();

            // Reset the feed index
            _feedIndex = -1;

            // Start the timer
            StartTimer();

            // Don't go further if we have no feeds
            if (feedCount == 0)
                return;

            // Get the first feed
            NextFeed();
        }

        private void NextFeed()
        {
            int feedCount = _database.Feeds.Count();

            if (feedCount == 0)
                return;

            if (Settings.Default.DisplayEmptyFeeds)
            {
                // Increment the index and adjust if we've gone around the end
                _feedIndex = (_feedIndex + 1) % feedCount;

                // Get the feed
                _currentFeed = _database.Feeds.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);
            }
            else
            {
                // Keep track if we found something
                bool found = false;

                // Remember our starting position
                int startIndex = (_feedIndex == -1 ? 0 : _feedIndex);

                // Increment the index and adjust if we've gone around the end
                _feedIndex = (_feedIndex + 1) % feedCount;

                // Loop until we come back to the start index
                do
                {
                    // Get the feed
                    _currentFeed = _database.Feeds.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);

                    // If the current feed has unread items then we can display it
                    if (_currentFeed.Items.Count(item => !item.BeenRead) > 0)
                    {
                        found = true;
                        break;
                    }

                    // Increment the index and adjust if we've gone around the end
                    _feedIndex = (_feedIndex + 1) % feedCount;
                }
                while (startIndex != _feedIndex);

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
            int feedCount = _database.Feeds.Count();

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
                _currentFeed = _database.Feeds.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);
            }
            else
            {
                // Keep track if we found something
                bool found = false;

                // Remember our starting position
                int startIndex = (_feedIndex == -1 ? 0 : _feedIndex);

                // Decrement the feed index
                _feedIndex--;

                // If we've gone below the start of the list then reset to the end
                if (_feedIndex < 0)
                    _feedIndex = feedCount - 1;

                // Loop until we come back to the start index
                do
                {
                    // Get the feed
                    _currentFeed = _database.Feeds.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex);

                    // If the current feed has unread items then we can display it
                    if (_currentFeed.Items.Count(item => !item.BeenRead) > 0)
                    {
                        found = true;
                        break;
                    }

                    // Decrement the feed index
                    _feedIndex--;

                    // If we've gone below the start of the list then reset to the end
                    if (_feedIndex < 0)
                        _feedIndex = feedCount - 1;
                }
                while (startIndex != _feedIndex);

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
            var multipleOpenAction = (MultipleOpenAction) _currentFeed.MultipleOpenAction;

            switch (multipleOpenAction)
            {
                case MultipleOpenAction.IndividualPages:
                    openAllToolbarButton.ToolTip = Properties.Resources.openAllMultipleToolbarButton;
                    break;
                case MultipleOpenAction.SinglePage:
                    openAllToolbarButton.ToolTip = Properties.Resources.openAllSingleToolbarButton;
                    break;
            }
        }

        private void DisplayFeed()
        {
            // Just clear the display if we have no feed
            if (_currentFeed == null)
            {
                feedLabel.Text = string.Empty;
                feedButton.Visibility = Visibility.Hidden;
                linkTextList.Items.Clear();

                return;
            }

            // Set the header to the feed title
            feedLabel.Text = (_currentFeed.Name.Length > 0 ? _currentFeed.Name : _currentFeed.Title);
            feedButton.Visibility = _database.Feeds.Count() > 1 ? Visibility.Visible : Visibility.Hidden;

            // Clear the current list
            linkTextList.Items.Clear();

            // Sort the items by sequence
            var sortedItems = _currentFeed.Items.Where(item => !item.BeenRead).OrderBy(item => item.Sequence);

            // Loop over all items in the current feed
            foreach (FeedItem feedItem in sortedItems)
            {
                // Add the list item
                linkTextList.Items.Add(feedItem);
            }

            UpdateOpenAllButton();
        }

        private void MarkAllItemsAsRead()
        {
            // Loop over all items and mark them as read
            foreach (FeedItem feedItem in linkTextList.Items)
                feedItem.BeenRead = true;

            // Save the changes
            _database.SaveChanges();

            // Clear the list
            linkTextList.Items.Clear();
        }

        #endregion

        #region Feed reading

        private class FeedReadWorkerInput
        {
            public bool ForceRead;
            public Feed Feed;
        }

        private class FeedReadWorkerOutput
        {
            public UpdateCheckInfo UpdateResult;
        }

        private void SetProgressMode(bool value, int feedCount)
        {
            // Reset the progress bar if we need it
            if (value)
            {
                feedReadProgress.Value = 0;
                feedReadProgress.Maximum = feedCount + 2;
                feedReadProgress.Visibility = Visibility.Visible;
            }
            else
            {
                feedReadProgress.Visibility = Visibility.Collapsed;
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
            FeedReadWorkerInput workerInput = new FeedReadWorkerInput { ForceRead = forceRead, Feed = _currentFeed };

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
            SetProgressMode(true, _database.Feeds.Count());

            // Create the input class
            FeedReadWorkerInput workerInput = new FeedReadWorkerInput { ForceRead = forceRead, Feed = null };

            // Start the worker
            _feedReadWorker.RunWorkerAsync(workerInput);
        }

        private void HandleFeedReadWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Set progress
            feedReadProgress.Value = e.ProgressPercentage;
        }

        private void HandleFeedReadWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Get the state info
            FeedReadWorkerOutput workerOutput = (FeedReadWorkerOutput) e.Result;

            // Reset the database to current settings
            ResetDatabase();

            // Save settings
            Settings.Default.Save();

            // Set the read timestamp
            _lastFeedRead = DateTime.Now;

            // Update the current feed
            DisplayFeed();

            // Switch to normal mode
            SetProgressMode(false, 0);

            // Check for update information
            UpdateCheckInfo updateCheckInfo = workerOutput.UpdateResult;

            if (updateCheckInfo != null && updateCheckInfo.UpdateAvailable)
                newVersionLink.Visibility = Visibility.Visible;

            UpdateErrorLink();
        }

        private void UpdateErrorLink()
        {
            var feedErrorCount = _database.Feeds.Count(f => f.LastReadResult != (int) FeedReadResult.Success);

            // Set the visibility of the error link
            feedErrorsLink.Visibility = feedErrorCount == 0 ? Visibility.Collapsed : Visibility.Visible;

            // Set the text to match the number of errors
            feedErrorsLink.Text = feedErrorCount == 1
                                      ? Properties.Resources.FeedErrorLink
                                      : string.Format(Properties.Resources.FeedErrorsLink, feedErrorCount);
        }

        private static void HandleFeedReadWorkerStart(object sender, DoWorkEventArgs e)
        {
            // Create a new database instance for just this thread
            FeedCenterEntities database = new FeedCenterEntities();

            // Get the worker
            BackgroundWorker worker = (BackgroundWorker) sender;

            // Get the input information
            FeedReadWorkerInput workerInput = (FeedReadWorkerInput) e.Argument;

            // Create the output 
            FeedReadWorkerOutput workerOutput = new FeedReadWorkerOutput();

            // Setup for progress
            int currentProgress = 0;

            // Create the list of feeds to read
            List<Feed> feedsToRead = new List<Feed>();

            // If we have a single feed then add it to the list - otherwise add them all
            if (workerInput.Feed != null)
                feedsToRead.Add(database.Feeds.First(feed => feed.ID == workerInput.Feed.ID));
            else
                feedsToRead.AddRange(database.Feeds);

            // Loop over each feed and read it
            foreach (Feed feed in feedsToRead)
            {
                // Read the feed
                feed.Read(database, workerInput.ForceRead);

                // Increment progress
                currentProgress += 1;

                // Report progress
                worker.ReportProgress(currentProgress);
            }

            // Save the changes
            database.SaveChanges();

            // Increment progress
            currentProgress += 1;

            // Report progress
            worker.ReportProgress(currentProgress);

            // See if we're due for a version check
            if (DateTime.Now - Settings.Default.LastVersionCheck >= Settings.Default.VersionCheckInterval && ApplicationDeployment.IsNetworkDeployed)
            {
                UpdateCheckInfo updateCheckInfo = null;

                try
                {
                    // Get the update information
                    updateCheckInfo = ApplicationDeployment.CurrentDeployment.CheckForDetailedUpdate(false);
                }
                catch (Exception exception)
                {
                    Tracer.WriteException(exception);
                }

                // Store the update information in the output
                workerOutput.UpdateResult = updateCheckInfo;

                // Update the last check time
                Settings.Default.LastVersionCheck = DateTime.Now;
            }

            // Increment progress
            currentProgress += 1;

            // Report progress
            worker.ReportProgress(currentProgress);

            // Save the output
            e.Result = workerOutput;

            // Sleep for a little bit so the user can see the update
            Thread.Sleep(Settings.Default.ProgressSleepInterval * 3);
        }

        #endregion

        #region Remote command line handling

        private void HandleCommandLine(object sender, InterprocessMessageListener.InterprocessMessageEventArgs e)
        {
            // If the command line is blank then ignore it
            if (e.Message.Length == 0)
                return;

            // Pad the command line with a trailing space just to be lazy in parsing
            string commandLine = e.Message + " ";

            // Look for the feed URL in the command line
            int startPosition = commandLine.IndexOf("feed://", StringComparison.Ordinal);

            // If we found one then we should extract and process it
            if (startPosition > 0)
            {
                // Advance past the protocol
                startPosition += 7;

                // Starting at the URL position look for the next space
                int endPosition = commandLine.IndexOf(" ", startPosition, StringComparison.Ordinal);

                // Extract the feed URL
                string feedUrl = commandLine.Substring(startPosition, endPosition - startPosition);

                // Add the HTTP protocol by default
                feedUrl = "http://" + feedUrl;

                // Create a new feed using the URL
                HandleNewFeed(feedUrl);
            }
        }

        private delegate void NewFeedDelegate(string feedUrl);
        private void HandleNewFeed(string feedUrl)
        {
            // Create and configure the new feed
            Feed feed = new Feed
            {
                Source = feedUrl,
                Category = _database.Categories.ToList().First(category => category.IsDefault),
            };

            // Read the feed for the first time
            FeedReadResult feedReadResult = feed.Read(_database);

            // See if we read the feed okay
            if (feedReadResult == FeedReadResult.Success)
            {
                // Update the feed name to be the title
                feed.Name = feed.Title;

                // Add the feed to the feed table
                _database.Feeds.AddObject(feed);

                // Save the changes
                _database.SaveChanges();

                // Show a tip
                NotificationIcon.ShowBalloonTip(string.Format(Properties.Resources.FeedAddedNotification, feed.Name), System.Windows.Forms.ToolTipIcon.Info);

                // Re-initialize the feed display
                DisplayFeed();
            }
            else
            {
                // Feed read failed - ceate a new feed window
                FeedWindow feedForm = new FeedWindow();

                bool? dialogResult = feedForm.Display(_database, feed, this);

                // Display the new feed form
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    // Add the feed to the feed table
                    _database.Feeds.AddObject(feed);

                    // Save the changes
                    _database.SaveChanges();

                    // Re-initialize the feed display
                    DisplayFeed();
                }
            }
        }

        #endregion

        #region Database helpers

        private void ResetDatabase()
        {
            // Get the ID of the current feed
            Guid currentId = _currentFeed.ID;

            // Create a new database object
            _database = new FeedCenterEntities();

            // Get a list of feeds ordered by name
            var feedList = _database.Feeds.OrderBy(f => f.Name).ToList();

            // First try to find the current feed by ID to see if it is still there
            var newIndex = feedList.FindIndex(f => f.ID == currentId);

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
            _currentFeed = (_feedIndex == -1 ? null : _database.Feeds.OrderBy(feed => feed.Name).AsEnumerable().ElementAt(_feedIndex));
        }

        #endregion

        #region Drag and drop

        private void HandleDragOver(object sender, DragEventArgs e)
        {
            // Default to not allowed
            e.Effects = DragDropEffects.None;
            e.Handled = true;

            // If there isn't any text in the data then it is not allowed
            if (!e.Data.GetDataPresent(DataFormats.Text))
                return;

            // Get the data as a string
            string data = (string) e.Data.GetData(DataFormats.Text);

            // If the data doesn't look like a URI then it is not allowed
            if (!Uri.IsWellFormedUriString(data, UriKind.Absolute))
                return;

            // Allowed
            e.Effects = DragDropEffects.Copy;
        }

        private void HandleDragDrop(object sender, DragEventArgs e)
        {
            // Get the data as a string
            string data = (string) e.Data.GetData(DataFormats.Text);

            // Handle the new feed but allow the drag/drop to complete
            Dispatcher.BeginInvoke(new NewFeedDelegate(HandleNewFeed), data);
        }

        #endregion

        #region Link list events

        private void HandleLinkTextListMouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1:

                    PreviousFeed();
                    break;

                case MouseButton.XButton2:

                    NextFeed();
                    break;
            }
        }

        private void HandleLinkTextListListItemMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Only handle the middle button
            if (e.ChangedButton != MouseButton.Middle)
                return;

            // Get the feed item
            FeedItem feedItem = (FeedItem) ((ListBoxItem) sender).DataContext;

            // The feed item has been read and is no longer new
            feedItem.BeenRead = true;
            feedItem.New = false;

            // Remove the item from the list
            linkTextList.Items.Remove(feedItem);

            // Save the changes
            _database.SaveChanges();

        }

        private void HandleLinkTextListListItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the feed item
            FeedItem feedItem = (FeedItem) ((ListBoxItem) sender).DataContext;

            // Open the item link
            if (BrowserCommon.OpenLink(feedItem.Link))
            {
                // The feed item has been read and is no longer new
                feedItem.BeenRead = true;
                feedItem.New = false;

                // Remove the item from the list
                linkTextList.Items.Remove(feedItem);

                // Save the changes
                _database.SaveChanges();
            }
        }

        #endregion

        #region Feed list menu

        private void HandleFeedButtonClick(object sender, RoutedEventArgs e)
        {
            // Create a new context menu
            ContextMenu contextMenu = new ContextMenu();

            // Loop over each feed
            foreach (Feed feed in _database.Feeds.OrderBy(feed => feed.Name))
            {
                // Build a string to display the feed name and the unread count
                string display = string.Format("{0} ({1:d})", feed.Name, feed.Items.Count(item => !item.BeenRead));

                // Create a menu item
                MenuItem menuItem = new MenuItem
                                        {
                                            Header = display,
                                            Tag = feed,

                                            // Set the current item to bold
                                            FontWeight = feed == _currentFeed ? FontWeights.Bold : FontWeights.Normal
                                        };


                // Handle the click
                menuItem.Click += HandleFeedMenuItemClick;

                // Add the item to the list
                contextMenu.Items.Add(menuItem);
            }

            // Set the context menu placement to this button
            contextMenu.PlacementTarget = this;

            // Open the context menu
            contextMenu.IsOpen = true;
        }

        private void HandleFeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            // Get the menu item clicked
            MenuItem menuItem = (MenuItem) sender;

            // Get the feed from the menu item tab
            Feed feed = (Feed) menuItem.Tag;

            // Loop over all feeds and look for the index of the new one		
            int feedIndex = 0;
            foreach (Feed loopFeed in _database.Feeds.OrderBy(loopFeed => loopFeed.Name))
            {
                if (loopFeed == feed)
                {
                    _feedIndex = feedIndex;
                    break;
                }

                feedIndex++;
            }

            // Set the current feed
            _currentFeed = feed;

            // Update the feed timestamp
            _lastFeedDisplay = DateTime.Now;

            // Update the display
            DisplayFeed();
        }

        #endregion

        #region Window border

        private void UpdateBorder()
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);

            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);

            System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle
                                                     {
                                                         X = (int) Left,
                                                         Y = (int) Top,
                                                         Width = (int) Width,
                                                         Height = (int) Height
                                                     };

            Thickness borderThickness = new Thickness();

            if (rectangle.Right != screen.WorkingArea.Right)
                borderThickness.Right = 1;

            if (rectangle.Left != screen.WorkingArea.Left)
                borderThickness.Left = 1;

            if (rectangle.Top != screen.WorkingArea.Top)
                borderThickness.Top = 1;

            if (rectangle.Bottom != screen.WorkingArea.Bottom)
                borderThickness.Bottom = 1;

            windowBorder.BorderThickness = borderThickness;
        }

        private DelayedMethod _windowStateDelay;

        private void HandleWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_windowStateDelay == null)
                _windowStateDelay = new DelayedMethod(500, UpdateWindowSettings);

            _windowStateDelay.Reset();
        }

        private void HandleWindowLocationChanged(object sender, EventArgs e)
        {
            if (_windowStateDelay == null)
                _windowStateDelay = new DelayedMethod(500, UpdateWindowSettings);

            _windowStateDelay.Reset();
        }

        private void UpdateWindowSettings()
        {
            // Save current window settings
            SaveWindowSettings();

            // Update the border
            UpdateBorder();
        }

        #endregion

        #region Feed header

        private void HandleFeedLabelMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open the link for the current feed on a left double click
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
                BrowserCommon.OpenLink(_currentFeed.Link);
        }

        #endregion

        #region Navigation toolbar

        private void HandlePreviousToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            PreviousFeed();
        }

        private void HandleNextToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            NextFeed();
        }

        private void OpenAllFeedItemsIndividually()
        {
            // Create a new list of feed items
            var feedItems = (from FeedItem feedItem in linkTextList.Items select feedItem).ToList();

            // Get the browser 
            Browser browser = BrowserCommon.FindBrowser(Settings.Default.Browser);

            // Cache the settings object
            var settings = Settings.Default;

            // Start with a longer sleep interval to give time for the browser to come up
            int sleepInterval = settings.OpenAllSleepIntervalFirst;

            // Loop over all items 
            foreach (FeedItem feedItem in feedItems)
            {
                // Try to open the link
                if (BrowserCommon.OpenLink(browser, feedItem.Link))
                {
                    // Mark the feed as read
                    feedItem.BeenRead = true;

                    // Remove the item
                    linkTextList.Items.Remove(feedItem);
                }

                // Wait a little bit
                Thread.Sleep(sleepInterval);

                // Switch to the normal sleep interval
                sleepInterval = settings.OpenAllSleepInterval;
            }

            // Save the changes
            _database.SaveChanges();
        }

        private void HandleOptionsToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            // Create the options form
            OptionsWindow optionsWindow = new OptionsWindow { Owner = this };

            // Show the options form and get the result
            bool? result = optionsWindow.ShowDialog();

            // If okay was selected
            if (result.HasValue && result.Value)
            {
                // Reset the database to current settings
                ResetDatabase();

                // Re-initialize the feed display
                DisplayFeed();
            }
        }

        private void HandleMarkReadToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            MarkAllItemsAsRead();
        }

        private void HandleShowErrorsButtonClick(object sender, RoutedEventArgs e)
        {
            // Create the feed error window
            FeedErrorWindow feedErrorWindow = new FeedErrorWindow();

            // Display the window
            bool? result = feedErrorWindow.Display(this);

            // If okay was selected
            if (result.GetValueOrDefault())
            {
                // Reset the database to current settings
                ResetDatabase();

                // Re-initialize the feed display
                DisplayFeed();

                UpdateErrorLink();
            }
        }

        private void HandleRefreshMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem) e.Source;

            if (menuItem == menuRefresh)
                ReadCurrentFeed(true);
            else if (menuItem == menuRefreshAll)
                ReadFeeds(true);
        }

        private void HandleRefreshToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            ReadFeeds(true);
        }

        private void HandleOpenAllMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem) e.Source;

            if (menuItem == menuOpenAllSinglePage)
                OpenAllFeedItemsOnSinglePage();
            else if (menuItem == menuOpenAllMultiplePages)
                OpenAllFeedItemsIndividually();
        }

        private void HandleOpenAllToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            var multipleOpenAction = (MultipleOpenAction) _currentFeed.MultipleOpenAction;

            switch (multipleOpenAction)
            {
                case MultipleOpenAction.IndividualPages:
                    OpenAllFeedItemsIndividually();
                    break;
                case MultipleOpenAction.SinglePage:
                    OpenAllFeedItemsOnSinglePage();
                    break;
            }
        }

        private void HandleEditCurrentFeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            // Create a new feed window
            FeedWindow feedWindow = new FeedWindow();

            // Display the feed window and get the result
            bool? result = feedWindow.Display(_database, _currentFeed, this);

            // If OK was clicked...
            if (result.HasValue && result.Value)
            {
                // Save
                _database.SaveChanges();

                // Update feed
                DisplayFeed();
            }
        }

        private void HandleDeleteCurrentFeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            // Confirm this delete since it is for real
            if (MessageBox.Show(this, Properties.Resources.ConfirmDelete, string.Empty, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                return;

            // Get the current feed
            Feed feedToDelete = _currentFeed;

            // Move to the next feed
            NextFeed();

            // Delete all items
            foreach (var item in feedToDelete.Items.ToList())
                _database.FeedItems.DeleteObject(item);

            // Delete the feed
            _database.Feeds.DeleteObject(feedToDelete);

            // Save
            _database.SaveChanges();
        }

        #endregion

        #region Single page reading

        private void OpenAllFeedItemsOnSinglePage()
        {
            string fileName = Path.GetTempFileName() + ".html";
            TextWriter textWriter = new StreamWriter(fileName);

            using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(textWriter))
            {
                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Html);

                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Head);

                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Title);
                htmlTextWriter.Write(_currentFeed.Title);
                htmlTextWriter.RenderEndTag();

                htmlTextWriter.AddAttribute("http-equiv", "Content-Type");
                htmlTextWriter.AddAttribute("content", "text/html; charset=utf-8");
                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Meta);
                htmlTextWriter.RenderEndTag();

                htmlTextWriter.RenderEndTag();

                htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Body);

                var sortedItems = from item in _currentFeed.Items where !item.BeenRead orderby item.Sequence ascending select item;

                bool firstItem = true;

                foreach (var item in sortedItems)
                {
                    if (!firstItem)
                    {
                        htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Hr);
                        htmlTextWriter.RenderEndTag();
                    }

                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                    htmlTextWriter.AddAttribute(HtmlTextWriterAttribute.Href, item.Link);
                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.A);
                    htmlTextWriter.Write(item.Title.Length == 0 ? item.Link : item.Title);
                    htmlTextWriter.RenderEndTag();

                    htmlTextWriter.RenderBeginTag(HtmlTextWriterTag.Br);
                    htmlTextWriter.RenderEndTag();

                    htmlTextWriter.Write(item.Description);

                    htmlTextWriter.RenderEndTag();

                    firstItem = false;
                }

                htmlTextWriter.RenderEndTag();
                htmlTextWriter.RenderEndTag();
            }

            textWriter.Flush();
            textWriter.Close();

            BrowserCommon.OpenLink(fileName);

            MarkAllItemsAsRead();
        }

        #endregion

        private void HandleNewVersionLinkClick(object sender, RoutedEventArgs e)
        {
            // Display update information
            VersionCheck.DisplayUpdateInformation(true);
        }

        #region Hover selection events

        private void HandleListItemMouseEnter(object sender, MouseEventArgs e)
        {
            // Get the list box item
            ListBoxItem listBoxItem = (ListBoxItem) sender;

            // Select the data context
            linkTextList.SelectedItem = listBoxItem.DataContext;

            // Set the cursor
            listBoxItem.Cursor = Cursors.Hand;
        }

        private void HandleListItemMouseLeave(object sender, MouseEventArgs e)
        {
            // Clear selection
            linkTextList.SelectedItem = null;
        }

        #endregion

    }
}
