namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Application;

public interface ICountersReadRepository
{
    Task<int?> GetCounterValue(string counterName);
}
