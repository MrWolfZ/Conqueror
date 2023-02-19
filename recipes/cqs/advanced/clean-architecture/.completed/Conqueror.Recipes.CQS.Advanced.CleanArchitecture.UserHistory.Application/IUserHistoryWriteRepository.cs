namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;

public interface IUserHistoryWriteRepository
{
    Task SetMostRecentlyIncrementedCounter(string userId, string counterName);
}
