namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

internal sealed class CountersRepository
{
    // we ignore thread-safety concerns for simplicity here, so we just use a simple dictionary
    private readonly Dictionary<string, int> counters = new();

    // we return tasks from these methods to more closely resemble an actual repository that talks to some database
    public async Task<IReadOnlyDictionary<string, int>> GetCounters()
    {
        await Task.CompletedTask;
        return counters;
    }
    
    public async Task<int> GetCounterValue(string counterName)
    {
        await Task.CompletedTask;
        return counters.TryGetValue(counterName, out var v) ? v : throw new CounterNotFoundException(counterName);
    }

    public async Task SetCounterValue(string counterName, int newValue)
    {
        await Task.CompletedTask;
        counters[counterName] = newValue;
    }

    public async Task DeleteCounter(string counterName)
    {
        await Task.CompletedTask;

        if (!counters.Remove(counterName))
        {
            throw new CounterNotFoundException(counterName);
        }
    }
}

// we use an exception to handle the case of a non-existing counter; there are other approaches to this
// as well (e.g. returning `null` or a boolean) but an exception allows for unified error handling
public sealed class CounterNotFoundException : Exception
{
    public CounterNotFoundException(string counterName)
    {
        CounterName = counterName;
    }

    public string CounterName { get; }
}
