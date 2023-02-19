namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

public interface IUserHistoryWriteRepository
{
    Task SetMostRecentlyIncrementedCounter(string userId, string counterName);
}
