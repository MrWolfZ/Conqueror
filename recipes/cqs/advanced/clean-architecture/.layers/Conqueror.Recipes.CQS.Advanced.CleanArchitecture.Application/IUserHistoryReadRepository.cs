namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

public interface IUserHistoryReadRepository
{
    Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId);
}
