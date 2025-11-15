using System;
using System.Linq;
using System.Threading.Tasks;
using ChrisKaczor.MinifluxClient;
using FeedCenter.Feeds;

namespace FeedCenter.Accounts;

internal class MinifluxReader(Account account) : IAccountReader
{
    public async Task<int> GetProgressSteps(AccountReadInput accountReadInput)
    {
        var minifluxClient = new MinifluxClient(account.Url, account.Password);

        int feedCount;
        if (accountReadInput.FeedId.HasValue)
        {
            feedCount = 1;
        }
        else
        {
            var feeds = await minifluxClient.GetFeeds();
            feedCount = feeds.Count();
        }

        return feedCount * 2 + 4;
    }

    public async Task<AccountReadResult> Read(AccountReadInput accountReadInput)
    {
        var checkTime = DateTimeOffset.UtcNow;

        var minifluxClient = new MinifluxClient(account.Url, account.Password);

        accountReadInput.IncrementProgress();

        var localFeeds = accountReadInput.Entities.Feeds.ToList();
        var remoteFeeds = (await minifluxClient.GetFeeds()).ToList();

        if (accountReadInput.FeedId.HasValue)
        {
            localFeeds = localFeeds.Where(f => f.Id == accountReadInput.FeedId.Value).ToList();
            remoteFeeds = remoteFeeds.Where(rf => rf.Id.ToString() == localFeeds.First().RemoteId).ToList();
        }
            
        accountReadInput.IncrementProgress();

        var transaction = accountReadInput.Entities.BeginTransaction();

        foreach (var remoteFeed in remoteFeeds)
        {
            var feed = accountReadInput.Entities.Feeds.FirstOrDefault(f => f.RemoteId == remoteFeed.Id.ToString() && f.Account.Id == account.Id);

            if (feed == null)
            {
                feed = new Feed
                {
                    Id = Guid.NewGuid(),
                    RemoteId = remoteFeed.Id.ToString(),
                    Title = remoteFeed.Title,
                    Source = remoteFeed.FeedUrl,
                    Link = remoteFeed.SiteUrl,
                    Account = account,
                    Name = remoteFeed.Title,
                    CategoryId = accountReadInput.Entities.DefaultCategory.Id,
                    Enabled = true,
                    CheckInterval = 0,
                };

                accountReadInput.Entities.Feeds.Add(feed);
            }

            feed.Name = remoteFeed.Title;
            feed.Title = remoteFeed.Title;
            feed.Link = remoteFeed.SiteUrl;
            feed.Source = remoteFeed.FeedUrl;
            feed.LastReadResult = FeedReadResult.Success;
            feed.LastChecked = checkTime;

            accountReadInput.IncrementProgress();

            var sequence = 1;

            var remoteFeedItems = (await minifluxClient.GetFeedEntries(remoteFeed.Id, SortField.PublishedAt, SortDirection.Descending)).ToList();

            foreach (var remoteFeedItem in remoteFeedItems)
            {
                var feedItem = feed.Items.FirstOrDefault(f => f.RemoteId == remoteFeedItem.Id.ToString());

                if (feedItem == null)
                {
                    feedItem = new FeedItem
                    {
                        Id = Guid.NewGuid(),
                        RemoteId = remoteFeedItem.Id.ToString(),
                        Title = remoteFeedItem.Title,
                        Link = remoteFeedItem.Url,
                        Description = remoteFeedItem.Content,
                        BeenRead = remoteFeedItem.Status == "read",
                        FeedId = feed.Id,
                        Guid = Guid.NewGuid().ToString(),
                        Sequence = sequence++,
                    };

                    feed.Items.Add(feedItem);
                }

                feedItem.LastFound = checkTime;
                feedItem.BeenRead = remoteFeedItem.Status == "read";
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

        var feedsNotSeen = localFeeds.Where(f => f.Account.Id == account.Id && f.LastChecked != checkTime).ToList();

        foreach (var feedNotSeen in feedsNotSeen)
        {
            accountReadInput.Entities.Feeds.Remove(feedNotSeen);
        }

        account.LastChecked = checkTime;

        await transaction.CommitAsync();

        accountReadInput.IncrementProgress();

        return AccountReadResult.Success;
    }

    public async Task MarkFeedItemRead(string feedItemId)
    {
        var minifluxClient = new MinifluxClient(account.Url, account.Password);

        await minifluxClient.MarkFeedEntries([long.Parse(feedItemId)], FeedEntryStatus.Read);
    }

    public bool SupportsFeedDelete => true;

    public bool SupportsFeedEdit => true;
}