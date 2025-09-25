using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChrisKaczor.FeverClient;

namespace FeedCenter;

internal class FeverReader : IAccountReader
{
    public int GetProgressSteps(FeedCenterEntities entities)
    {
        return 7;
    }

    public AccountReadResult Read(Account account, AccountReadInput accountReadInput)
    {
        var checkTime = DateTimeOffset.UtcNow;

        var apiKey = account.Authenticate ? GetApiKey(account) : string.Empty;

        var feverClient = new FeverClient(account.Url, apiKey);

        accountReadInput.IncrementProgress();

        var feverFeeds = feverClient.GetFeeds().Result.ToList();

        accountReadInput.IncrementProgress();

        var allFeverFeedItems = feverClient.GetAllFeedItems().Result.ToList();

        accountReadInput.IncrementProgress();

        var transaction = accountReadInput.Entities.BeginTransaction();

        foreach (var feverFeed in feverFeeds)
        {
            var feed = accountReadInput.Entities.Feeds.FirstOrDefault(f => f.RemoteId == feverFeed.Id.ToString() && f.Account.Id == account.Id);

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

            var feverFeedItems = allFeverFeedItems
                .Where(f => f.FeedId == feverFeed.Id)
                .OrderByDescending(fi => fi.CreatedOnTime).ToList();

            var sequence = 1;

            foreach (var feverFeedItem in feverFeedItems)
            {
                var feedItem = feed.Items.FirstOrDefault(f => f.RemoteId == feverFeedItem.Id.ToString());

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

        transaction.Commit();

        accountReadInput.IncrementProgress();

        return AccountReadResult.Success;
    }

    public static async Task MarkFeedItemRead(Account account, string feedItemId)
    {
        var apiKey = account.Authenticate ? GetApiKey(account) : string.Empty;

        var feverClient = new FeverClient(account.Url, apiKey);

        await feverClient.MarkFeedItemAsRead(int.Parse(feedItemId));
    }

    private static string GetApiKey(Account account)
    {
        var input = $"{account.Username}:{account.Password}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}