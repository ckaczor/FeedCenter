using System;

namespace FeedCenter;

public class AccountReadInput(FeedCenterEntities entities, Guid? feedId, bool forceRead, Action incrementProgress)
{
    public FeedCenterEntities Entities { get; set; } = entities;
    public Guid? FeedId { get; set; } = feedId;
    public bool ForceRead { get; set; } = forceRead;
    public Action IncrementProgress { get; private set; } = incrementProgress ?? throw new ArgumentNullException(nameof(incrementProgress));
}