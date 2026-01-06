namespace Conqueror.Recipes.Messaging.TestingHandlers;

internal class CountersRepository
{
    private readonly Dictionary<string, int> counters = new();

    public async Task<int> GetCounterValue(string counterName)
    {
        await Task.CompletedTask;
        return counters.TryGetValue(counterName, out var v)
            ? v
            : throw new CounterNotFoundException(counterName);
    }

    public async Task SetCounterValue(string counterName, int newValue)
    {
        await Task.CompletedTask;
        counters[counterName] = newValue;
    }
}

public class CounterNotFoundException(string counterName) : Exception
{
    public string CounterName { get; } = counterName;
}
