namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;

public interface IUserHistoryReadRepository
{
    Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId);
}
