namespace FeedCenter;

public interface IAccountReader
{
    public int GetProgressSteps(FeedCenterEntities entities);
    public AccountReadResult Read(Account account, AccountReadInput accountReadInput);
}