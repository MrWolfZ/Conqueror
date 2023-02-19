namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

public interface ICountersReadRepository
{
    Task<int?> GetCounterValue(string counterName);
}
