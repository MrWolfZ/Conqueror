namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

public interface ICountersWriteRepository
{
    Task SetCounterValue(string counterName, int newValue);
}
