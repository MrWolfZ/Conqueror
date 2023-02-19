namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;

public interface IUserHistoryReadRepository
{
    Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId);
}
