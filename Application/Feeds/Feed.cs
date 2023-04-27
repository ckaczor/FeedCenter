using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Data;
using FeedCenter.FeedParsers;
using FeedCenter.Properties;
using FeedCenter.Xml;
using JetBrains.Annotations;
using Realms;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace FeedCenter;

public partial class Feed : RealmObject, INotifyDataErrorInfo
{
    private static HttpClient _httpClient;

    private readonly DataErrorDictionary _dataErrorDictionary;

    public Feed()
    {
        _dataErrorDictionary = new DataErrorDictionary();
        _dataErrorDictionary.ErrorsChanged += DataErrorDictionaryErrorsChanged;
    }

    public bool Authenticate { get; set; }
    public Guid CategoryId { get; set; }
    public int CheckInterval { get; set; } = 60;
    public string Description { get; set; }
    public bool Enabled { get; set; } = true;

    [PrimaryKey]
    public Guid Id { get; set; }

    [UsedImplicitly]
    public IList<FeedItem> Items { get; }

    public DateTimeOffset LastChecked { get; set; }

    public FeedReadResult LastReadResult
    {
        get => Enum.TryParse(LastReadResultRaw, out FeedReadResult result) ? result : FeedReadResult.Success;
        set => LastReadResultRaw = value.ToString();
    }

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

    private string LastReadResultRaw { get; set; }

    public DateTimeOffset LastUpdated { get; set; }
    public string Link { get; set; }

    public MultipleOpenAction MultipleOpenAction
    {
        get => Enum.TryParse(MultipleOpenActionRaw, out MultipleOpenAction result) ? result : MultipleOpenAction.IndividualPages;
        set => MultipleOpenActionRaw = value.ToString();
    }

    private string MultipleOpenActionRaw { get; set; }

    public string Name
    {
        get => RawName;
        set
        {
            RawName = value;

            ValidateString(nameof(Name), RawName);
            RaisePropertyChanged();
        }
    }

    [MapTo("Password")]
    public string RawPassword { get; set; }

    public string Password
    {
        get => RawPassword;
        set
        {
            RawPassword = value;

            if (!Authenticate)
            {
                _dataErrorDictionary.ClearErrors(nameof(Password));
                return;
            }

            ValidateString(nameof(Password), RawPassword);
            RaisePropertyChanged();
        }
    }

    [MapTo("Name")]
    private string RawName { get; set; } = string.Empty;

    [MapTo("Source")]
    private string RawSource { get; set; } = string.Empty;

    public string Source
    {
        get => RawSource;
        set
        {
            RawSource = value;

            ValidateString(nameof(Source), RawSource);
            RaisePropertyChanged();
        }
    }

    public string Title { get; set; }

    [MapTo("Username")]
    public string RawUsername { get; set; }

    public string Username
    {
        get => RawUsername;
        set
        {
            RawUsername = value;

            if (!Authenticate)
            {
                _dataErrorDictionary.ClearErrors(nameof(Username));
                return;
            }

            ValidateString(nameof(Username), RawUsername);
            RaisePropertyChanged();
        }
    }

    public bool HasErrors => _dataErrorDictionary.Any();

    public IEnumerable GetErrors(string propertyName)
    {
        return _dataErrorDictionary.GetErrors(propertyName);
    }

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public static Feed Create()
    {
        return new Feed { Id = Guid.NewGuid(), CategoryId = Database.Entities.DefaultCategory.Id };
    }

    private void DataErrorDictionaryErrorsChanged(object sender, DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(e.PropertyName));
    }

    private void ValidateString(string propertyName, string value)
    {
        _dataErrorDictionary.ClearErrors(propertyName);

        if (string.IsNullOrWhiteSpace(value))
            _dataErrorDictionary.AddError(propertyName, $"{propertyName} cannot be empty");
    }

    public FeedReadResult Read(bool forceRead = false)
    {
        Log.Logger.Information("Reading feed: {0}", Source);

        var result = ReadFeed(forceRead);

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

        var feedType = FeedType.Unknown;

        try
        {
            feedType = FeedParserBase.DetectFeedType(retrieveResult.Item2);
        }
        catch
        {
            // Ignore
        }

        return new Tuple<FeedType, string>(feedType, retrieveResult.Item2);
    }

    private Tuple<FeedReadResult, string> RetrieveFeed()
    {
        try
        {
            // Create and configure the HTTP client if needed
            if (_httpClient == null)
            {
                var clientHandler = new HttpClientHandler
                {
                    // Set that we'll accept compressed data
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = true
                };

                _httpClient = new HttpClient(clientHandler);

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

            if (string.IsNullOrEmpty(feedText))
                return Tuple.Create(FeedReadResult.NoResponse, string.Empty);

            // Get rid of any leading and trailing whitespace
            feedText = feedText.Trim();

            // Clean up common invalid XML characters
            feedText = feedText.Replace("&nbsp;", "&#160;");

            // Find ampersands that aren't properly escaped and replace them with escaped versions
            var r = UnescapedAmpersandRegex();
            feedText = r.Replace(feedText, "&amp;");

            return Tuple.Create(FeedReadResult.Success, feedText);
        }
        catch (IOException ioException)
        {
            Log.Logger.Error(ioException, "Exception");

            return Tuple.Create(FeedReadResult.ConnectionFailed, string.Empty);
        }
        catch (AggregateException aggregateException)
        {
            Log.Logger.Error(aggregateException, "Exception");

            var innerException = aggregateException.InnerException;

            if (innerException is not HttpRequestException httpRequestException)
                return Tuple.Create(FeedReadResult.UnknownError, string.Empty);

            switch (httpRequestException.StatusCode)
            {
                case HttpStatusCode.ServiceUnavailable:
                    return Tuple.Create(FeedReadResult.TemporarilyUnavailable, string.Empty);

                case HttpStatusCode.InternalServerError:
                    return Tuple.Create(FeedReadResult.ServerError, string.Empty);

                case HttpStatusCode.NotModified:
                    return Tuple.Create(FeedReadResult.NotModified, string.Empty);

                case HttpStatusCode.NotFound:
                    return Tuple.Create(FeedReadResult.NotFound, string.Empty);

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    return Tuple.Create(FeedReadResult.Unauthorized, string.Empty);

                case HttpStatusCode.Moved:
                    return Tuple.Create(FeedReadResult.Moved, string.Empty);
            }

            if (httpRequestException.InnerException is not SocketException socketException)
                return Tuple.Create(FeedReadResult.UnknownError, string.Empty);

            return socketException.SocketErrorCode switch
            {
                SocketError.NoData => Tuple.Create(FeedReadResult.NoResponse, string.Empty),
                SocketError.HostNotFound => Tuple.Create(FeedReadResult.NotFound, string.Empty),
                _ => Tuple.Create(FeedReadResult.UnknownError, string.Empty)
            };
        }
        catch (WebException webException)
        {
            var result = FeedReadResult.UnknownError;

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

    private FeedReadResult ReadFeed(bool forceRead)
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
        catch (FeedParseException feedParseException)
        {
            Log.Logger.Error(feedParseException, "Exception");

            return FeedReadResult.InvalidXml;
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

    [GeneratedRegex("&(?!(?:[a-z]+|#[0-9]+|#x[0-9a-f]+);)")]
    private static partial Regex UnescapedAmpersandRegex();
}