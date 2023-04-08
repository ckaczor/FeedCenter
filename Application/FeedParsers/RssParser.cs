﻿using FeedCenter.Xml;
using Serilog;
using System;
using System.Xml;

namespace FeedCenter.FeedParsers
{
    internal class RssParser : FeedParserBase
    {
        public RssParser(Feed feed) : base(feed) { }

        public override FeedReadResult ParseFeed(string feedText)
        {
            try
            {
                // Create the XML document
                var document = new XmlDocument { XmlResolver = null };

                // Load the XML document from the text
                document.LoadXml(feedText);

                // Create the namespace manager
                var namespaceManager = document.GetAllNamespaces();

                // Get the root node
                XmlNode rootNode = document.DocumentElement;

                // If we didn't find a root node then bail
                if (rootNode == null)
                    return FeedReadResult.UnknownError;

                // Get the channel node
                var channelNode = rootNode.SelectSingleNode("default:channel", namespaceManager) ??
                                  rootNode.SelectSingleNode("channel", namespaceManager);

                if (channelNode == null)
                    return FeedReadResult.InvalidXml;

                // Initialize the sequence number for items
                var sequence = 0;

                // Loop over all nodes in the channel node
                foreach (XmlNode node in channelNode.ChildNodes)
                {
                    // Handle each node that we find
                    switch (node.Name)
                    {
                        case "title":
                            Feed.Title = System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();
                            break;

                        case "link":
                            Feed.Link = node.InnerText.Trim();
                            break;

                        case "description":
                            Feed.Description = node.InnerText;
                            break;

                        case "item":
                            HandleFeedItem(node, ref sequence);
                            break;
                    }
                }

                return FeedReadResult.Success;
            }
            catch (XmlException xmlException)
            {
                Log.Logger.Error(xmlException, "Exception: {0}", feedText);

                return FeedReadResult.InvalidXml;
            }
        }

        protected override FeedItem ParseFeedItem(XmlNode node)
        {
            // Create a new feed item
            var feedItem = FeedItem.Create();

            // Loop over all nodes in the feed node
            foreach (XmlNode childNode in node.ChildNodes)
            {
                // Handle each node that we find
                switch (childNode.Name.ToLower())
                {
                    case "title":
                        feedItem.Title = System.Net.WebUtility.HtmlDecode(childNode.InnerText).Trim();
                        break;

                    case "link":
                        feedItem.Link = childNode.InnerText.Trim();
                        break;

                    case "guid":
                        feedItem.Guid = childNode.InnerText.Trim();

                        var permaLink = true;

                        if (childNode.Attributes != null)
                        {
                            var permaLinkNode = childNode.Attributes.GetNamedItem("isPermaLink");
                            permaLink = (permaLinkNode == null || permaLinkNode.Value == "true");
                        }

                        if (permaLink && Uri.IsWellFormedUriString(feedItem.Guid, UriKind.Absolute))
                            feedItem.Link = feedItem.Guid;

                        break;

                    case "description":
                        feedItem.Description = System.Net.WebUtility.HtmlDecode(childNode.InnerText);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(feedItem.Guid))
                feedItem.Guid = feedItem.Link;

            return feedItem;
        }
    }
}
