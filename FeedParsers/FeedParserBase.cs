using System;
using System.Linq;
using System.Xml;

using Common.Debug;

namespace FeedCenter.FeedParsers
{
    [Serializable]
    internal class InvalidFeedFormatException : ApplicationException
    {
        internal InvalidFeedFormatException(Exception exception) : base(string.Empty, exception)
        {            
        }
    }

    internal abstract class FeedParserBase
    {
        #region Member variables

        protected readonly Feed Feed;

        #endregion

        #region Constructor

        protected FeedParserBase(Feed feed)
        {
            Feed = feed;
        }

        #endregion

        #region Methods

        public abstract FeedReadResult ParseFeed(string feedText);

        protected abstract FeedItem ParseFeedItem(XmlNamespaceManager namespaceManager, XmlNode node);

        protected void HandleFeedItem(XmlNamespaceManager namespaceManager, XmlNode node, ref int sequence)
        {
            // Build a feed item from the node
            FeedItem newFeedItem = ParseFeedItem(namespaceManager, node);

            if (newFeedItem == null)
                return;

            // Check for feed items with no guid or link
            if (string.IsNullOrWhiteSpace(newFeedItem.Guid) && string.IsNullOrWhiteSpace(newFeedItem.Link))
                return;

            // Look for an item that has the same guid
            FeedItem existingFeedItem = Feed.Items.FirstOrDefault(item => item.Guid == newFeedItem.Guid && item.ID != newFeedItem.ID);

            // Check to see if we already have this feed item
            if (existingFeedItem == null)
            {
                Tracer.WriteLine("New link: " + newFeedItem.Link);

                // Associate the new item with the right feed
                newFeedItem.Feed = Feed;

                // Set the item as new
                newFeedItem.New = true;

                // Add the item to the list
                Feed.Items.Add(newFeedItem);

                // Feed was updated
                Feed.LastUpdated = DateTime.Now;
            }
            else
            {
                Tracer.WriteLine("Existing link: " + newFeedItem.Link);

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

        #endregion

        #region Parser creation and detection

        public static FeedParserBase CreateFeedParser(Feed feed, string feedText)
        {
            FeedType feedType = DetectFeedType(feedText);

            switch (feedType)
            {
                case FeedType.Rss:
                    return new RssParser(feed);

                case FeedType.Rdf:
                    return new RdfParser(feed);

                case FeedType.Atom:
                    return new AtomParser(feed);
            }

            throw new ArgumentException(string.Format("Feed type {0} is not supported", feedType));
        }

        public static FeedType DetectFeedType(string feedText)
        {
            try
            {
                // Create the XML document
                XmlDocument document = new XmlDocument { XmlResolver = null };

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
            catch (Exception exception)
            {
                throw new InvalidFeedFormatException(exception);
            }
        }

        #endregion
    }
}
