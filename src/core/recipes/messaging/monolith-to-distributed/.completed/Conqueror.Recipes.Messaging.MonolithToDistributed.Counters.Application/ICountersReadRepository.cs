namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;

public interface ICountersReadRepository
{
    Task<int?> GetCounterValue(string counterName);
}
