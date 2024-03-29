﻿using Serilog;
using System;
using System.Linq;
using System.Xml;

namespace FeedCenter.FeedParsers;

internal abstract class FeedParserBase
{
    protected readonly Feed Feed;

    protected FeedParserBase(Feed feed)
    {
        Feed = feed;
    }

    public abstract FeedReadResult ParseFeed(string feedText);

    protected abstract FeedItem ParseFeedItem(XmlNode node);

    protected void HandleFeedItem(XmlNode node, ref int sequence)
    {
        // Build a feed item from the node
        var newFeedItem = ParseFeedItem(node);

        if (newFeedItem == null)
            return;

        // Check for feed items with no guid or link
        if (string.IsNullOrWhiteSpace(newFeedItem.Guid) && string.IsNullOrWhiteSpace(newFeedItem.Link))
            return;

        // Look for an item that has the same guid
        var existingFeedItem = Feed.Items.FirstOrDefault(item => item.Guid == newFeedItem.Guid && item.Id != newFeedItem.Id);

        // Check to see if we already have this feed item
        if (existingFeedItem == null)
        {
            Log.Logger.Information("New link: " + newFeedItem.Link);

            // Set the item as new
            newFeedItem.New = true;

            // Add the item to the list
            Feed.Items.Add(newFeedItem);

            // Feed was updated
            Feed.LastUpdated = DateTime.Now;
        }
        else
        {
            Log.Logger.Information("Existing link: " + newFeedItem.Link);

            // Update the fields in the existing item
            existingFeedItem.Link = newFeedItem.Link;
            existingFeedItem.Title = newFeedItem.Title;
            existingFeedItem.Guid = newFeedItem.Guid;
            existingFeedItem.Description = newFeedItem.Description;

            // Item is no longer new
            existingFeedItem.New = false;

            // Switch over to the existing item for the rest
            newFeedItem = existingFeedItem;
        }

        // Item was last seen now
        newFeedItem.LastFound = Feed.LastChecked;

        // Set the sequence
        newFeedItem.Sequence = sequence;

        // Increment the sequence
        sequence++;
    }

    public static FeedParserBase CreateFeedParser(Feed feed, string feedText)
    {
        var feedType = DetectFeedType(feedText);

        return feedType switch
        {
            FeedType.Rss => new RssParser(feed),
            FeedType.Rdf => new RdfParser(feed),
            FeedType.Atom => new AtomParser(feed),
            _ => throw new ArgumentException($"Feed type {feedType} is not supported")
        };
    }

    public static FeedType DetectFeedType(string feedText)
    {
        try
        {
            // Create the XML document
            var document = new XmlDocument { XmlResolver = null };

            // Load the XML document from the text
            document.LoadXml(feedText);

            // Loop over all child nodes
            foreach (XmlNode node in document.ChildNodes)
            {
                switch (node.Name)
                {
                    case "rss":
                        return FeedType.Rss;

                    case "rdf:RDF":
                        return FeedType.Rdf;

                    case "feed":
                        return FeedType.Atom;
                }
            }

            // No clue!
            return FeedType.Unknown;
        }
        catch (XmlException xmlException)
        {
            Log.Logger.Error(xmlException, "Exception: {0}", feedText);

            throw new FeedParseException(FeedParseError.InvalidXml);
        }
        catch (Exception exception)
        {
            Log.Logger.Error(exception, "Exception: {0}", feedText);

            throw new FeedParseException(FeedParseError.InvalidXml);
        }
    }
}