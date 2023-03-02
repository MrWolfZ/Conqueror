namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;

public interface ICountersReadRepository
{
    Task<int?> GetCounterValue(string counterName);
}
