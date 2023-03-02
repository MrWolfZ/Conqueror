namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;

public interface IUserHistoryWriteRepository
{
    Task SetMostRecentlyIncrementedCounter(string userId, string counterName);
}
