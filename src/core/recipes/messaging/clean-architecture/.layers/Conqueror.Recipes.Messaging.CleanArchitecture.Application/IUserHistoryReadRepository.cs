namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

public interface IUserHistoryReadRepository
{
    Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId);
}
