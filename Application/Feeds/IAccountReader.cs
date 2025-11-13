using System.Threading.Tasks;

namespace FeedCenter.Feeds;

public interface IAccountReader
{
    public Task<int> GetProgressSteps(AccountReadInput accountReadInput);
    public Task<AccountReadResult> Read(AccountReadInput accountReadInput);
    public Task MarkFeedItemRead(string feedItemId);
}