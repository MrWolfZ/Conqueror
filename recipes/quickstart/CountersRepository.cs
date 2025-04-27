using System.Collections.Concurrent;

namespace Quickstart;

// simulate a database repository (which is usually async)
internal sealed class CountersRepository
{
    private readonly ConcurrentDictionary<string, long> counters = new();

    public async Task<long> AddOrIncrementCounter(string counterName, long incrementBy)
    {
        await Task.Yield();
        return counters.AddOrUpdate(counterName, incrementBy, (_, value) => value + incrementBy);
    }

    public async Task<long> GetCounterValue(string counterName)
    {
        await Task.Yield();
        return counters.GetValueOrDefault(counterName, 0L);
    }

    public async Task<IReadOnlyDictionary<string, long>> GetCounters()
    {
        await Task.Yield();
        return counters;
    }
}
