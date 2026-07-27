namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

public interface ICountersReadRepository
{
    Task<int?> GetCounterValue(string counterName);
}
