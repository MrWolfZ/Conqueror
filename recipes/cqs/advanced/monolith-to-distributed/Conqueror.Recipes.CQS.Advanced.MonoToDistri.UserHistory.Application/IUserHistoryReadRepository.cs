namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;

public interface IUserHistoryReadRepository
{
    Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId);
}
