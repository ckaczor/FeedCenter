using ChrisKaczor.FeverClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FeedCenter.Feeds;

namespace FeedCenter.Accounts;

using FeverFeedItem = ChrisKaczor.FeverClient.Models.FeedItem;

internal class FeverReader(Account account) : IAccountReader
{
    public async Task<int> GetProgressSteps(AccountReadInput accountReadInput)
    {
        var apiKey = account.Authenticate ? GetApiKey(account) : string.Empty;

        var feverClient = new FeverClient(account.Url, apiKey);

        var feeds = await feverClient.GetFeeds();

        return feeds.Count() * 2 + 5;
    }

    public async Task<AccountReadResult> Read(AccountReadInput accountReadInput)
    {
        var checkTime = DateTimeOffset.UtcNow;

        var apiKey = account.Authenticate ? GetApiKey(account) : string.Empty;

        var feverClient = new FeverClient(account.Url, apiKey);

        accountReadInput.IncrementProgress();

        var feverFeeds = (await feverClient.GetFeeds()).ToList();

        accountReadInput.IncrementProgress();

        var allFeverFeedItems = await GetAllFeverFeedItems(feverClient);

        accountReadInput.IncrementProgress();

        var existingFeedsByRemoteId = accountReadInput.Entities.Feeds.Where(f => f.Account.Id == account.Id) .ToDictionary(f => f.RemoteId);

        var transaction = accountReadInput.Entities.BeginTransaction();

        foreach (var feverFeed in feverFeeds)
        {
            var feed = existingFeedsByRemoteId.GetValueOrDefault(feverFeed.Id.ToString(), null);

            if (feed == null)
            {
                feed = new Feed
                {
                    Id = Guid.NewGuid(),
                    RemoteId = feverFeed.Id.ToString(),
                    Title = feverFeed.Title,
                    Source = feverFeed.Url,
                    Link = feverFeed.SiteUrl,
                    Account = account,
                    Name = feverFeed.Title,
                    CategoryId = accountReadInput.Entities.DefaultCategory.Id,
                    Enabled = true,
                    CheckInterval = 0,
                };

                accountReadInput.Entities.Feeds.Add(feed);
            }

            feed.Name = feverFeed.Title;
            feed.Title = feverFeed.Title;
            feed.Link = feverFeed.SiteUrl;
            feed.Source = feverFeed.Url;
            feed.LastReadResult = FeedReadResult.Success;
            feed.LastChecked = checkTime;

            accountReadInput.IncrementProgress();

            var feverFeedItems = allFeverFeedItems.GetValueOrDefault(feverFeed.Id, []);

            var existingFeedItemsByRemoteId = feed.Items.ToDictionary(fi => fi.RemoteId);

            var sequence = 1;

            foreach (var feverFeedItem in feverFeedItems)
            {
                var feedItem = existingFeedItemsByRemoteId.GetValueOrDefault(feverFeedItem.Id.ToString(), null);

                if (feedItem == null)
                {
                    feedItem = new FeedItem
                    {
                        Id = Guid.NewGuid(),
                        RemoteId = feverFeedItem.Id.ToString(),
                        Title = feverFeedItem.Title,
                        Link = feverFeedItem.Url,
                        Description = feverFeedItem.Html,
                        BeenRead = feverFeedItem.IsRead,
                        FeedId = feed.Id,
                        Guid = Guid.NewGuid().ToString(),
                        Sequence = sequence++,
                    };

                    feed.Items.Add(feedItem);
                }

                feedItem.LastFound = checkTime;
                feedItem.BeenRead = feverFeedItem.IsRead;
                feedItem.Sequence = sequence++;
            }

            accountReadInput.IncrementProgress();

            var feedItemsNotSeen = feed.Items.Where(fi => fi.LastFound != checkTime).ToList();

            foreach (var feedItemNotSeen in feedItemsNotSeen)
            {
                feed.Items.Remove(feedItemNotSeen);
            }
        }

        accountReadInput.IncrementProgress();

        var feedsNotSeen = accountReadInput.Entities.Feeds.Where(f => f.Account.Id == account.Id && f.LastChecked != checkTime).ToList();

        foreach (var feedNotSeen in feedsNotSeen)
        {
            accountReadInput.Entities.Feeds.Remove(feedNotSeen);
        }

        account.LastChecked = checkTime;

        await transaction.CommitAsync();

        accountReadInput.IncrementProgress();

        return AccountReadResult.Success;
    }

    private static async Task<Dictionary<int, List<FeverFeedItem>>> GetAllFeverFeedItems(FeverClient feverClient)
    {
        var allFeverFeedItems = new List<FeverFeedItem>();

        await foreach (var page in feverClient.GetAllFeedItems())
        {
            allFeverFeedItems.AddRange(page);
        }

        var grouped = allFeverFeedItems.OrderByDescending(fi => fi.CreatedOnTime).GroupBy(fi => fi.FeedId);

        return grouped.ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task MarkFeedItemRead(string feedItemId)
    {
        var apiKey = account.Authenticate ? GetApiKey(account) : string.Empty;

        var feverClient = new FeverClient(account.Url, apiKey);

        await feverClient.MarkFeedItemAsRead(int.Parse(feedItemId));
    }

    public bool SupportsFeedDelete => false;

    public bool SupportsFeedEdit => false;

    private static string GetApiKey(Account account)
    {
        var input = $"{account.Username}:{account.Password}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}