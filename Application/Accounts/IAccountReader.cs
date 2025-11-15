using System.Threading.Tasks;

namespace FeedCenter.Accounts;

public interface IAccountReader
{
    public Task<int> GetProgressSteps(AccountReadInput accountReadInput);
    public Task<AccountReadResult> Read(AccountReadInput accountReadInput);
    public Task MarkFeedItemRead(string feedItemId);
    public bool SupportsFeedDelete { get; }
    public bool SupportsFeedEdit { get; }
}