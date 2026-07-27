namespace Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Application;

public interface ICountersWriteRepository
{
    Task SetCounterValue(string counterName, int newValue);
}
