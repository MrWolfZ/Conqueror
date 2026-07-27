namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;

public interface ICountersWriteRepository
{
    Task SetCounterValue(string counterName, int newValue);
}
