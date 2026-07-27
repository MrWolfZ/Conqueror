namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

public interface IUserHistoryWriteRepository
{
    Task SetMostRecentlyIncrementedCounter(string userId, string counterName);
}
