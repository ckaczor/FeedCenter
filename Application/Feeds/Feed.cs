using System.Text.RegularExpressions;
using Common.Debug;
using Common.Xml;
using FeedCenter.FeedParsers;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Data;

namespace FeedCenter
{
    #region Enumerations

    public enum MultipleOpenAction
    {
        IndividualPages,
        SinglePage
    }

    public enum FeedType
    {
        Unknown,
        Rss,
        Rdf,
        Atom
    }

    public enum FeedItemComparison : byte
    {
        Default,
        Title
    }

    public enum FeedReadResult
    {
        Success,
        NotModified,
        NotDue,
        UnknownError,
        InvalidXml,
        NotEnabled,
        Unauthorized,
        NoResponse,
        NotFound,
        Timeout,
        ConnectionFailed,
        ServerError
    }

    #endregion

    public partial class Feed
    {
        public static Feed Create()
        {
            return new Feed { ID = Guid.NewGuid() };
        }

        #region Event delegates

        public delegate void ErrorEventHandler(WebException webException);

        #endregion

        #region Reading

        public FeedReadResult Read(FeedCenterEntities database, bool forceRead = false)
        {
            Tracer.WriteLine("Reading feed: {0}", Source);
            Tracer.IncrementIndentLevel();

            var result = ReadFeed(database, forceRead);

            // Handle the result
            switch (result)
            {
                case FeedReadResult.NotDue:
                case FeedReadResult.NotEnabled:
                case FeedReadResult.NotModified:
                    // Ignore
                    break;

                default:
                    // Save as last result
                    LastReadResult = result;

                    break;
            }

            // If the feed was successfully read and we have no last update timestamp - set the last update timestamp to now
            if (result == FeedReadResult.Success && LastUpdated == Data.Extensions.SqlDateTimeZero.Value)
                LastUpdated = DateTime.Now;

            Tracer.DecrementIndentLevel();
            Tracer.WriteLine("Done reading feed: {0}", result);

            return result;
        }

        private Tuple<FeedReadResult, string> RetrieveFeed()
        {
            try
            {
                // Create the web request
                var oRequest = WebRequest.Create(new Uri(Source));

                // Attempt to cast to a web request
                var webRequest = oRequest as HttpWebRequest;

                // If this is an http request set some special properties
                if (webRequest != null)
                {
                    // Make sure to use HTTP version 1.1
                    webRequest.ProtocolVersion = HttpVersion.Version11;

                    // Set that we'll accept compressed data
                    webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                    // Set a timeout
                    webRequest.Timeout = 10000;

                    // If we need to authenticate then set the credentials
                    if (Authenticate)
                        webRequest.Credentials = new NetworkCredential(Username, Password, Domain);

                    // Set a user agent string
                    webRequest.UserAgent = "FeedCenter 1.0 ALPHA";
                }

                // Set the default encoding
                var encoding = Encoding.UTF8;

                // Attempt to get the response
                var response = (HttpWebResponse) oRequest.GetResponse();

                // If the response included an encoding then change the encoding
                if (response.ContentEncoding.Length > 0)
                    encoding = Encoding.GetEncoding(response.ContentEncoding);

                // Get the response stream
                var responseStream = response.GetResponseStream();

                if (responseStream == null)
                    return Tuple.Create(FeedReadResult.NoResponse, string.Empty);

                // Create the text reader
                StreamReader textReader = new XmlSanitizingStream(responseStream, encoding);

                // Get the feed text
                var feedText = textReader.ReadToEnd();

                // Get rid of any leading and trailing whitespace
                feedText = feedText.Trim();

                // Clean up common invalid XML characters
                feedText = feedText.Replace("&nbsp;", "&#160;");

                // Find ampersands that aren't properly escaped and replace them with escaped versions
                var r = new Regex("&(?!(?:[a-z]+|#[0-9]+|#x[0-9a-f]+);)");
                feedText = r.Replace(feedText, "&amp;");

                return Tuple.Create(FeedReadResult.Success, feedText);
            }
            catch (IOException ioException)
            {
                Tracer.WriteLine(ioException.Message);

                return Tuple.Create(FeedReadResult.ConnectionFailed, string.Empty);
            }
            catch (WebException webException)
            {
                var result = FeedReadResult.UnknownError;

                var errorResponse = webException.Response as HttpWebResponse;

                if (errorResponse != null)
                {
                    switch (errorResponse.StatusCode)
                    {
                        case HttpStatusCode.InternalServerError:
                            return Tuple.Create(FeedReadResult.ServerError, string.Empty);

                        case HttpStatusCode.NotModified:
                            return Tuple.Create(FeedReadResult.NotModified, string.Empty);

                        case HttpStatusCode.NotFound:
                            return Tuple.Create(FeedReadResult.NotFound, string.Empty);

                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                            return Tuple.Create(FeedReadResult.Unauthorized, string.Empty);
                    }
                }

                switch (webException.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.NameResolutionFailure:
                        result = FeedReadResult.ConnectionFailed;
                        break;

                    case WebExceptionStatus.Timeout:
                        result = FeedReadResult.Timeout;
                        break;
                }

                Tracer.WriteException(webException);

                if (result == FeedReadResult.UnknownError)
                    Debug.Print("Unknown error");

                return Tuple.Create(result, string.Empty);
            }
            catch (Exception exception)
            {
                Tracer.WriteLine(exception.Message);

                return Tuple.Create(FeedReadResult.UnknownError, string.Empty);
            }
        }

        private FeedReadResult ReadFeed(FeedCenterEntities database, bool forceRead)
        {
            try
            {
                // If not enabled then do nothing
                if (!Enabled)
                    return FeedReadResult.NotEnabled;

                // Check if we're forcing a read
                if (!forceRead)
                {
                    // Figure out how long since we last checked
                    var timeSpan = DateTime.Now - LastChecked;

                    // Check if we are due to read the feed
                    if (timeSpan.TotalMinutes < CheckInterval)
                        return FeedReadResult.NotDue;
                }

                // We're checking it now so update the time
                LastChecked = DateTime.Now;

                // Read the feed text
                var retrieveResult = RetrieveFeed();

                // Get the information out of the async result
                var result = retrieveResult.Item1;
                var feedText = retrieveResult.Item2;

                // If we didn't successfully retrieve the feed then stop
                if (result != FeedReadResult.Success)
                    return result;

                // Create a new RSS parser
                var feedParser = FeedParserBase.CreateFeedParser(this, feedText);

                // Parse the feed
                result = feedParser.ParseFeed(feedText);

                // If we didn't successfully parse the feed then stop
                if (result != FeedReadResult.Success)
                    return result;

                // Create the removed items list - if an item wasn't seen during this check then remove it
                var removedItems = Items.Where(testItem => testItem.LastFound != LastChecked).ToList();

                // If items were removed the feed was updated
                if (removedItems.Count > 0)
                    LastUpdated = DateTime.Now;

                // Loop over the items to be removed
                foreach (var itemToRemove in removedItems)
                {
                    // Delete the item from the database
                    database.FeedItems.Remove(itemToRemove);

                    // Remove the item from the list
                    Items.Remove(itemToRemove);
                }

                // Process actions on this feed
                ProcessActions();

                return FeedReadResult.Success;
            }
            catch (InvalidFeedFormatException exception)
            {
                Tracer.WriteException(exception.InnerException);

                return FeedReadResult.InvalidXml;
            }
            catch (Exception exception)
            {
                Tracer.WriteLine(exception.Message);

                return FeedReadResult.UnknownError;
            }
        }

        private void ProcessActions()
        {
            var sortedActions = from action in Actions orderby action.Sequence ascending select action;

            foreach (var feedAction in sortedActions)
            {
                switch (feedAction.Field)
                {
                    case 0:
                        Title = Title.Replace(feedAction.Search, feedAction.Replace);
                        break;
                }
            }
        }

        #endregion

        public string LastReadResultDescription
        {
            get
            {
                // Cast the last read result to the proper enum
                var lastReadResult = LastReadResult;

                // Build the name of the resource using the enum name and the value
                var resourceName = string.Format("{0}_{1}", typeof(FeedReadResult).Name, lastReadResult);

                // Try to get the value from the resources
                var resourceValue = Properties.Resources.ResourceManager.GetString(resourceName);

                // Return the value or just the enum value if not found
                return resourceValue ?? lastReadResult.ToString();
            }
        }
    }
}
