using Common.Debug;
using Common.Xml;
using System.Xml;

namespace FeedCenter.FeedParsers
{
    internal class AtomParser : FeedParserBase
    {
        public AtomParser(Feed feed) : base(feed) { }

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

                // Initialize the sequence number for items
                var sequence = 0;

                // Loop over all nodes in the root node
                foreach (XmlNode node in rootNode.ChildNodes)
                {
                    // Handle each node that we find
                    switch (node.Name)
                    {
                        case "title":
                            Feed.Title = System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();
                            break;

                        case "link":
                            string rel = null;

                            if (node.Attributes == null)
                                break;

                            XmlNode relNode = node.Attributes["rel"];

                            if (relNode != null)
                                rel = relNode.InnerText;

                            if (string.IsNullOrEmpty(rel) || rel == "alternate")
                                Feed.Link = node.Attributes["href"].InnerText.Trim();

                            break;

                        case "subtitle":
                            Feed.Description = node.InnerText;
                            break;

                        case "entry":
                            HandleFeedItem(namespaceManager, node, ref sequence);
                            break;
                    }
                }

                return FeedReadResult.Success;
            }
            catch (XmlException xmlException)
            {
                Tracer.WriteLine("XML error: " + xmlException.Message + "\n" + feedText);

                return FeedReadResult.InvalidXml;
            }
        }

        protected override FeedItem ParseFeedItem(XmlNamespaceManager namespaceManager, XmlNode node)
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

                    case "id":
                        feedItem.Guid = childNode.InnerText;
                        break;

                    case "content":
                        feedItem.Description = System.Net.WebUtility.HtmlDecode(childNode.InnerText);
                        break;

                    case "link":
                        string rel = null;

                        if (childNode.Attributes == null)
                            break;

                        XmlNode relNode = childNode.Attributes["rel"];

                        if (relNode != null)
                            rel = relNode.InnerText.Trim();

                        if (string.IsNullOrEmpty(rel) || rel == "alternate")
                        {
                            var link = childNode.Attributes["href"].InnerText;

                            if (link.StartsWith("/"))
                            {
                                link = Feed.Link + link;
                                link = link.Replace("//", "/");
                            }

                            feedItem.Link = link;
                        }

                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(feedItem.Guid))
                feedItem.Guid = feedItem.Link;

            return feedItem;
        }
    }
}
