namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Infrastructure;

internal sealed class UserHistoryRepository : IUserHistoryReadRepository, IUserHistoryWriteRepository
{
    // we ignore thread-safety concerns for simplicity here, so we just use a simple dictionary
    private readonly Dictionary<string, string> mostRecentIncrementedCounterNameByUserId = new();

    public async Task<string?> GetMostRecentlyIncrementedCounterByUserId(string userId)
    {
        await Task.CompletedTask;
        return mostRecentIncrementedCounterNameByUserId.TryGetValue(userId, out var v) ? v : null;
    }

    public async Task SetMostRecentlyIncrementedCounter(string userId, string counterName)
    {
        await Task.CompletedTask;
        mostRecentIncrementedCounterNameByUserId[userId] = counterName;
    }
}
