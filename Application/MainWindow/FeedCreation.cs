﻿using FeedCenter.Options;
using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace FeedCenter;

public partial class MainWindow
{
    private delegate void NewFeedDelegate(string feedUrl);

    private static string GetAbsoluteUrlString(string baseUrl, string url)
    {
        var uri = new Uri(url, UriKind.RelativeOrAbsolute);
        if (!uri.IsAbsoluteUri)
            uri = new Uri(new Uri(baseUrl), uri);
        return uri.ToString();
    }

    private void HandleNewFeed(string feedUrl)
    {
        // Create and configure the new feed
        var feed = Feed.Create();
        feed.Source = feedUrl;
        feed.CategoryId = _database.DefaultCategory.Id;

        // Try to detect the feed type
        var feedTypeResult = feed.DetectFeedType();

        // If we can't figure it out it could be an HTML page
        if (feedTypeResult.Item1 == FeedType.Unknown)
        {
            // Only check if the feed was able to be read - otherwise fall through and show the dialog
            if (feedTypeResult.Item2.Length > 0)
            {
                // Create and load an HTML document with the text
                var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(feedTypeResult.Item2);

                // Look for all RSS or atom links in the document
                var rssLinks = htmlDocument.DocumentNode.Descendants("link")
                    .Where(n => n.Attributes["type"] != null && (n.Attributes["type"].Value == "application/rss+xml" || n.Attributes["type"].Value == "application/atom+xml"))
                    .Select(n => new Tuple<string, string>(GetAbsoluteUrlString(feed.Source, n.Attributes["href"].Value), WebUtility.HtmlDecode(n.Attributes["title"]?.Value ?? feedUrl)))
                    .Distinct()
                    .ToList();

                // If there was only one link found then switch to feed to it
                if (rssLinks.Count == 1)
                {
                    feed.Source = rssLinks[0].Item1;
                }
                else
                {
                    var feedChooserWindow = new FeedChooserWindow();
                    var feedLink = feedChooserWindow.Display(this, rssLinks);

                    if (string.IsNullOrEmpty(feedLink))
                        return;

                    feed.Source = feedLink;
                }
            }
        }

        // Read the feed for the first time
        var feedReadResult = feed.Read(true);

        // Check to see if this might be rate limited
        if (feedReadResult == FeedReadResult.TemporarilyUnavailable)
        {
            // Wait a second
            Thread.Sleep(1000);

            // Try to read again
            feedReadResult = feed.Read(true);
        }

        // See if we read the feed okay
        if (feedReadResult == FeedReadResult.Success)
        {
            // Update the feed name to be the title
            feed.Name = feed.Title;

            // Add the feed to the feed table
            _database.SaveChanges(() => _database.Feeds.Add(feed));

            // Show a tip
            NotificationIcon.ShowBalloonTip(string.Format(Properties.Resources.FeedAddedNotification, feed.Name), H.NotifyIcon.Core.NotificationIcon.Info);

            _currentFeed = feed;

            // Refresh the database to current settings
            ResetDatabase();

            // Re-initialize the feed display
            DisplayFeed();
        }
        else
        {
            // Feed read failed - create a new feed window
            var feedForm = new FeedWindow();

            var dialogResult = feedForm.Display(feed, this);

            // Display the new feed form
            if (!dialogResult.HasValue || !dialogResult.Value)
                return;

            // Add the feed to the feed table
            _database.SaveChanges(() => _database.Feeds.Add(feed));

            _currentFeed = feed;

            // Refresh the database to current settings
            ResetDatabase();

            // Re-initialize the feed display
            DisplayFeed();
        }
    }
}