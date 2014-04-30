using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Common.Debug;
using Common.Extensions;
using Common.Xml;
using FeedCenter.Data;
using FeedCenter.FeedParsers;
using System.Threading.Tasks;

namespace FeedCenter
{
    #region Enumerations

    public enum FeedType
    {
        Unknown,
        Rss,
        Rdf,
        Atom
    }

    public enum FeedItemComparison
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
        #region Constructor

        public Feed()
        {
            ID = Guid.NewGuid();
        }

        #endregion

        #region Event delegates

        public delegate void ErrorEventHandler(WebException webException);

        #endregion

        #region Reading

        public FeedReadResult Read(FeedCenterEntities database, bool forceRead = false)
        {
            Tracer.WriteLine("Reading feed: {0}", Source);
            Tracer.IncrementIndentLevel();

            FeedReadResult result = ReadFeed(database, forceRead);

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
                    LastReadResult = (int) result;

                    break;
            }

            // If the feed was successfully read and we have no last update timestamp - set the last update timestamp to now
            if (result == FeedReadResult.Success && LastUpdated == FeedCenter.Data.Extensions.SqlDateTimeZero.Value)
                LastUpdated = DateTime.Now;

            Tracer.DecrementIndentLevel();
            Tracer.WriteLine("Done reading feed: {0}", result);

            return result;
        }

        private async Task<Tuple<FeedReadResult, string>> RetrieveFeed()
        {
            try
            {
                // Create the web request
                WebRequest oRequest = WebRequest.Create(new Uri(Source));

                // If this is an http request set some special properties
                if (oRequest is HttpWebRequest)
                {
                    // Cast the request
                    HttpWebRequest webRequest = (HttpWebRequest) oRequest;

                    // Make sure to use HTTP version 1.1
                    webRequest.ProtocolVersion = HttpVersion.Version11;

                    // Set that we'll accept compressed data
                    webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                    // If we need to authenticate then set the credentials
                    if (Authenticate)
                        webRequest.Credentials = new NetworkCredential(Username, Password, Domain);

                    // Set a user agent string
                    webRequest.UserAgent = "FeedCenter 1.0 ALPHA";
                }

                // Set the default encoding
                Encoding encoding = Encoding.UTF8;
                
                // Attempt to get the response
                var response = (HttpWebResponse) await oRequest.GetResponseAsync().WithTimeout(10000).ConfigureAwait(false);

                // If there was no response assume it was a timeout of the async method
                if (response == null)
                    return Tuple.Create(FeedReadResult.Timeout, string.Empty);

                // If the response included an encoding then change the encoding
                if (response.ContentEncoding.Length > 0)
                    encoding = Encoding.GetEncoding(response.ContentEncoding);

                // Get the response stream
                Stream responseStream = response.GetResponseStream();

                if (responseStream == null)
                    return Tuple.Create(FeedReadResult.NoResponse, string.Empty);

                // Create the text reader
                StreamReader textReader = new XmlSanitizingStream(responseStream, encoding);

                // Get the feed text
                string feedText = textReader.ReadToEnd();

                // Get rid of any leading and trailing whitespace
                feedText = feedText.Trim();

                // Clean up common invalid XML characters
                feedText = feedText.Replace("&nbsp;", "&#160;");

                return Tuple.Create(FeedReadResult.Success, feedText);
            }
            catch (IOException ioException)
            {
                Tracer.WriteLine(ioException.Message);

                return Tuple.Create(FeedReadResult.ConnectionFailed, string.Empty);
            }
            catch (WebException webException)
            {
                FeedReadResult result = FeedReadResult.UnknownError;

                if (webException.Response is HttpWebResponse)
                {
                    HttpWebResponse errorResponse = (HttpWebResponse) webException.Response;

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
                string feedText;

                // If not enabled then do nothing
                if (!Enabled)
                    return FeedReadResult.NotEnabled;

                // Check if we're forcing a read
                if (!forceRead)
                {
                    // Figure out how long since we last checked
                    TimeSpan timeSpan = DateTime.Now - LastChecked;

                    // Check if we are due to read the feed
                    if (timeSpan.TotalMinutes < CheckInterval)
                        return FeedReadResult.NotDue;
                }

                // We're checking it now so update the time
                LastChecked = DateTime.Now;

                // Read the feed text
                var retrieveResult = RetrieveFeed();

                // Wait on the result
                retrieveResult.Wait();

                // Get the information out of the async result
                FeedReadResult result = retrieveResult.Result.Item1;
                feedText = retrieveResult.Result.Item2;

                // If we didn't successfully retrieve the feed then stop
                if (result != FeedReadResult.Success)
                    return result;

                // Create a new RSS parser
                FeedParserBase feedParser = FeedParserBase.CreateFeedParser(this, feedText);

                // Parse the feed
                result = feedParser.ParseFeed(feedText);

                // If we didn't successfully parse the feed then stop
                if (result != FeedReadResult.Success)
                    return result;

                // Create the removed items list - if an item wasn't seen during this check then remove it
                List<FeedItem> removedItems = Items.Where(testItem => testItem.LastFound != LastChecked).ToList();

                // If items were removed the feed was updated
                if (removedItems.Count > 0)
                    LastUpdated = DateTime.Now;

                // Loop over the items to be removed
                foreach (FeedItem itemToRemove in removedItems)
                {
                    // Delete the item from the database
                    database.FeedItems.DeleteObject(itemToRemove);

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

            foreach (FeedAction feedAction in sortedActions)
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
                var lastReadResult = (FeedReadResult) LastReadResult;

                // Build the name of the resource using the enum name and the value
                string resourceName = string.Format("{0}_{1}", typeof(FeedReadResult).Name, lastReadResult);

                // Try to get the value from the resources
                string resourceValue = Properties.Resources.ResourceManager.GetString(resourceName);

                // Return the value or just the enum value if not found
                return resourceValue ?? lastReadResult.ToString();
            }
        }
    }
}
