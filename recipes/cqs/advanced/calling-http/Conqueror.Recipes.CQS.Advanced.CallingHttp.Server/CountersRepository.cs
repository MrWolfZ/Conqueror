namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

internal sealed class CountersRepository
{
    // we ignore thread-safety concerns for simplicity here, so we just use a simple dictionary
    private readonly Dictionary<string, int> counters = new();

    private int callCounter;

    public async Task<int?> GetCounterValue(string counterName)
    {
        await Task.CompletedTask;
        return counters.TryGetValue(counterName, out var v) ? v : null;
    }

    public async Task SetCounterValue(string counterName, int newValue)
    {
        callCounter += 1;

        if (callCounter % 3 == 0)
        {
            throw new IOException("simulate transient network error");
        }

        await Task.CompletedTask;
        counters[counterName] = newValue;
    }
}
