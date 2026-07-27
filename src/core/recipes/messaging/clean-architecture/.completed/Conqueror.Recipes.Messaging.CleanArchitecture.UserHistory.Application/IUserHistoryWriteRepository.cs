namespace Conqueror.Recipes.Messaging.CleanArchitecture.UserHistory.Application;

public interface IUserHistoryWriteRepository
{
    Task SetMostRecentlyIncrementedCounter(string userId, string counterName);
}
