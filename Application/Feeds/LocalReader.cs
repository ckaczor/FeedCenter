using System;
using System.Collections.Generic;
using System.Linq;

namespace FeedCenter;

public class LocalReader : IAccountReader
{
    public int GetProgressSteps(FeedCenterEntities entities)
    {
        var enabledFeedCount = entities.Feeds.Count(f => f.Account.Type == AccountType.Local && f.Enabled);

        return enabledFeedCount;
    }

    public AccountReadResult Read(Account account, AccountReadInput accountReadInput)
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

        return AccountReadResult.Success;
    }
}