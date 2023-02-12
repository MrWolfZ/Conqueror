namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

internal sealed class CountersRepository
{
    // we ignore thread-safety concerns for simplicity here, so we just use a simple dictionary
    private readonly Dictionary<string, int> counters = new();

    public async Task<int?> GetCounterValue(string counterName)
    {
        await Task.CompletedTask;
        return counters.TryGetValue(counterName, out var v) ? v : null;
    }

    public async Task SetCounterValue(string counterName, int newValue)
    {
        await Task.CompletedTask;
        counters[counterName] = newValue;
    }
}
