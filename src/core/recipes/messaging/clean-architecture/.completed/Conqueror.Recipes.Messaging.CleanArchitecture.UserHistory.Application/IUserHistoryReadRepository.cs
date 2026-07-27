namespace Conqueror.Recipes.Messaging.CleanArchitecture.UserHistory.Application;

public interface IUserHistoryReadRepository
{
    Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId);
}
