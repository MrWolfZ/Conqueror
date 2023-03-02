namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;

public interface ICountersWriteRepository
{
    Task SetCounterValue(string counterName, int newValue);
}
