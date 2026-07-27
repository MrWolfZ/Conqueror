namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;

public interface IUserHistoryWriteRepository
{
    Task SetMostRecentlyIncrementedCounter(string userId, string counterName);
}
