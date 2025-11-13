using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeedCenter.Feeds;

public class LocalReader(Account account) : IAccountReader
{
    public Task<int> GetProgressSteps(AccountReadInput accountReadInput)
    {
        var enabledFeedCount = accountReadInput.Entities.Feeds.Count(f => f.Account.Type == AccountType.Local && f.Enabled);

        return Task.FromResult(enabledFeedCount);
    }

    public Task<AccountReadResult> Read(AccountReadInput accountReadInput)
    {
        var checkTime = DateTimeOffset.UtcNow;

        // Create the list of feeds to read
        var feedsToRead = new List<Feed>();

        // If we have a single feed then add it to the list - otherwise add them all
        if (accountReadInput.FeedId != null)
            feedsToRead.Add(accountReadInput.Entities.Feeds.First(feed => feed.Id == accountReadInput.FeedId));
        else
            feedsToRead.AddRange(accountReadInput.Entities.Feeds.Where(f => f.Account.Type == AccountType.Local));

        // Loop over each feed and read it
        foreach (var feed in feedsToRead)
        {
            // Read the feed
            accountReadInput.Entities.SaveChanges(() => feed.Read(accountReadInput.ForceRead));

            accountReadInput.IncrementProgress();
        }

        accountReadInput.Entities.SaveChanges(() => account.LastChecked = checkTime);

        return Task.FromResult(AccountReadResult.Success);
    }

    public Task MarkFeedItemRead(string feedItemId)
    {
        throw new NotImplementedException();
    }
}