namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

public interface ICountersWriteRepository
{
    Task SetCounterValue(string counterName, int newValue);
}
