namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Application;

public interface ICountersWriteRepository
{
    Task SetCounterValue(string counterName, int newValue);
}
