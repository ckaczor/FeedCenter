using ChrisKaczor.InstalledBrowsers;
using FeedCenter.Properties;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FeedCenter
{
    public partial class MainWindow
    {
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

        private void HandleItemMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Only handle the middle button
            if (e.ChangedButton != MouseButton.Middle)
                return;

            // Get the feed item
            var feedItem = (FeedItem) ((ListBoxItem) sender).DataContext;

            // The feed item has been read and is no longer new
            _database.SaveChanges(() =>
            {
                feedItem.BeenRead = true;
                feedItem.New = false;
            });

            // Remove the item from the list
            LinkTextList.Items.Remove(feedItem);
        }

        private void HandleItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the feed item
            var feedItem = (FeedItem) ((ListBoxItem) sender).DataContext;

            // Try to open the item link
            if (!InstalledBrowser.OpenLink(Settings.Default.Browser, feedItem.Link))
                return;

            // The feed item has been read and is no longer new
            _database.SaveChanges(() =>
            {
                feedItem.BeenRead = true;
                feedItem.New = false;
            });

            // Remove the item from the list
            LinkTextList.Items.Remove(feedItem);
        }

        private void HandleFeedButtonClick(object sender, RoutedEventArgs e)
        {
            // Create a new context menu
            var contextMenu = new ContextMenu();

            // Loop over each feed
            foreach (var feed in _feedList.OrderBy(feed => feed.Name))
            {
                // Build a string to display the feed name and the unread count
                var display = $"{feed.Name} ({feed.Items.Count(item => !item.BeenRead):d})";

                // Create a menu item
                var menuItem = new MenuItem
                {
                    Header = display,
                    Tag = feed,

                    // Set the current item to bold
                    FontWeight = feed.Id == _currentFeed.Id ? FontWeights.Bold : FontWeights.Normal
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
            var menuItem = (MenuItem) sender;

            // Get the feed from the menu item tab
            var feed = (Feed) menuItem.Tag;

            // Loop over all feeds and look for the index of the new one		
            var feedIndex = 0;
            foreach (var loopFeed in _feedList.OrderBy(loopFeed => loopFeed.Name))
            {
                if (loopFeed.Id == feed.Id)
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
    }
}