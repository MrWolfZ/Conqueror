namespace Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Application;

public interface ICountersReadRepository
{
    Task<int?> GetCounterValue(string counterName);
}
