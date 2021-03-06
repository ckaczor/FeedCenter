﻿using FeedCenter.Options;
using FeedCenter.Properties;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;

namespace FeedCenter
{
    public partial class MainWindow
    {
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
            var feedItems = (from FeedItem feedItem in LinkTextList.Items select feedItem).ToList();

            // Get the browser 
            var browser = BrowserCommon.FindBrowser(Settings.Default.Browser);

            // Cache the settings object
            var settings = Settings.Default;

            // Start with a longer sleep interval to give time for the browser to come up
            var sleepInterval = settings.OpenAllSleepIntervalFirst;

            // Loop over all items 
            foreach (var feedItem in feedItems)
            {
                // Try to open the link
                if (BrowserCommon.OpenLink(browser, feedItem.Link))
                {
                    // Mark the feed as read
                    feedItem.BeenRead = true;

                    // Remove the item
                    LinkTextList.Items.Remove(feedItem);
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
            var optionsWindow = new OptionsWindow { Owner = this };

            // Show the options form and get the result
            var result = optionsWindow.ShowDialog();

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
            var feedErrorWindow = new FeedErrorWindow();

            // Display the window
            var result = feedErrorWindow.Display(this);

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
            var menuItem = (MenuItem) e.Source;

            if (Equals(menuItem, MenuRefresh))
                ReadCurrentFeed(true);
            else if (Equals(menuItem, MenuRefreshAll))
                ReadFeeds(true);
        }

        private void HandleRefreshToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            ReadFeeds(true);
        }

        private void HandleOpenAllMenuItemClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem) e.Source;

            if (Equals(menuItem, MenuOpenAllSinglePage))
                OpenAllFeedItemsOnSinglePage();
            else if (Equals(menuItem, MenuOpenAllMultiplePages))
                OpenAllFeedItemsIndividually();
        }

        private void HandleOpenAllToolbarButtonClick(object sender, RoutedEventArgs e)
        {
            var multipleOpenAction = _currentFeed.MultipleOpenAction;

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
            var feedWindow = new FeedWindow();

            // Display the feed window and get the result
            var result = feedWindow.Display(_database, _currentFeed, this);

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
            var feedToDelete = _currentFeed;

            // Move to the next feed
            NextFeed();

            // Delete all items
            foreach (var item in feedToDelete.Items.ToList())
                _database.FeedItems.Remove(item);

            // Delete the feed
            _database.Feeds.Remove(feedToDelete);

            // Save
            _database.SaveChanges();
        }

        private void OpenAllFeedItemsOnSinglePage()
        {
            var fileName = Path.GetTempFileName() + ".html";
            TextWriter textWriter = new StreamWriter(fileName);

            using (var htmlTextWriter = new HtmlTextWriter(textWriter))
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

                var firstItem = true;

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
    }
}
