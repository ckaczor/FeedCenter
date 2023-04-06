﻿using CKaczor.ApplicationUpdate;
using FeedCenter.Data;
using FeedCenter.FeedParsers;
using FeedCenter.Properties;
using Realms;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FeedCenter.Xml;
using Resources = FeedCenter.Properties.Resources;

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

    public class Feed : RealmObject
    {
        [PrimaryKey]
        [MapTo("ID")]
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Title { get; set; }
        public string Source { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTimeOffset LastChecked { get; set; }
        public int CheckInterval { get; set; } = 60;
        public bool Enabled { get; set; } = true;
        public bool Authenticate { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }

        private string LastReadResultRaw { get; set; }

        public FeedReadResult LastReadResult
        {
            get => Enum.TryParse(LastReadResultRaw, out FeedReadResult result) ? result : FeedReadResult.Success;
            set => LastReadResultRaw = value.ToString();
        }

        public DateTimeOffset LastUpdated { get; set; }

        private string ItemComparisonRaw { get; set; }

        public FeedItemComparison ItemComparison
        {
            get => Enum.TryParse(ItemComparisonRaw, out FeedItemComparison result) ? result : FeedItemComparison.Default;
            set => ItemComparisonRaw = value.ToString();
        }

        [MapTo("CategoryID")]
        public Guid CategoryId { get; set; }

        private string MultipleOpenActionRaw { get; set; }

        public MultipleOpenAction MultipleOpenAction
        {
            get => Enum.TryParse(MultipleOpenActionRaw, out MultipleOpenAction result) ? result : MultipleOpenAction.IndividualPages;
            set => MultipleOpenActionRaw = value.ToString();
        }

        public Category Category { get; set; }

        public IList<FeedItem> Items { get; }

        // ReSharper disable once UnusedMember.Global
        public string LastReadResultDescription
        {
            get
            {
                // Cast the last read result to the proper enum
                var lastReadResult = LastReadResult;

                // Build the name of the resource using the enum name and the value
                var resourceName = $"{nameof(FeedReadResult)}_{lastReadResult}";

                // Try to get the value from the resources
                var resourceValue = Resources.ResourceManager.GetString(resourceName);

                // Return the value or just the enum value if not found
                return resourceValue ?? lastReadResult.ToString();
            }
        }

        private static HttpClient _httpClient;

        public static Feed Create(FeedCenterEntities database)
        {
            return new Feed { Id = Guid.NewGuid(), CategoryId = database.DefaultCategory.Id };
        }

        #region Reading

        public FeedReadResult Read(FeedCenterEntities database, bool forceRead = false)
        {
            Log.Logger.Information("Reading feed: {0}", Source);

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
            if (result == FeedReadResult.Success && LastUpdated == default)
                LastUpdated = DateTimeOffset.Now;

            Log.Logger.Information("Done reading feed: {0}", result);

            return result;
        }

        public Tuple<FeedType, string> DetectFeedType()
        {
            var retrieveResult = RetrieveFeed();

            if (retrieveResult.Item1 != FeedReadResult.Success)
            {
                return new Tuple<FeedType, string>(FeedType.Unknown, string.Empty);
            }

            return new Tuple<FeedType, string>(FeedParserBase.DetectFeedType(retrieveResult.Item2), retrieveResult.Item2);
        }

        private Tuple<FeedReadResult, string> RetrieveFeed()
        {
            try
            {
                // Create and configure the HTTP client if needed
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient(new HttpClientHandler
                    {
                        // Set that we'll accept compressed data
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    });

                    // Set a user agent string
                    var userAgent = string.IsNullOrWhiteSpace(Settings.Default.DefaultUserAgent) ? "FeedCenter/" + UpdateCheck.LocalVersion : Settings.Default.DefaultUserAgent;
                    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

                    // Set a timeout
                    _httpClient.Timeout = TimeSpan.FromSeconds(10);
                }

                // If we need to authenticate then set the credentials
                _httpClient.DefaultRequestHeaders.Authorization = Authenticate ? new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"))) : null;

                // Attempt to get the response
                var feedStream = _httpClient.GetStreamAsync(Source).Result;

                // Create the text reader
                using StreamReader textReader = new XmlSanitizingStream(feedStream, Encoding.UTF8);

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
                Log.Logger.Error(ioException, "Exception");

                return Tuple.Create(FeedReadResult.ConnectionFailed, string.Empty);
            }
            catch (WebException webException)
            {
                var result = FeedReadResult.UnknownError;

                if (webException.Response is HttpWebResponse errorResponse)
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

                Log.Logger.Error(webException, "Exception");

                if (result == FeedReadResult.UnknownError)
                    Debug.Print("Unknown error");

                return Tuple.Create(result, string.Empty);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Exception");

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
                    var timeSpan = DateTimeOffset.Now - LastChecked;

                    // Check if we are due to read the feed
                    if (timeSpan.TotalMinutes < CheckInterval)
                        return FeedReadResult.NotDue;
                }

                // We're checking it now so update the time
                LastChecked = DateTimeOffset.Now;

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
                    // Remove the item from the list
                    Items.Remove(itemToRemove);
                }

                return FeedReadResult.Success;
            }
            catch (InvalidFeedFormatException exception)
            {
                Log.Logger.Error(exception, "Exception");

                return FeedReadResult.InvalidXml;
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Exception");

                return FeedReadResult.UnknownError;
            }
        }

        #endregion
    }
}