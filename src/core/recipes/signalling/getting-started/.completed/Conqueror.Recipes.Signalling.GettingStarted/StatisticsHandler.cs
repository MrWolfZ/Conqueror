namespace Conqueror.Recipes.Signalling.GettingStarted;

internal partial class StatisticsHandler(CounterStatistics statistics) : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        statistics.RecordedIncrements++;
        Console.WriteLine($"statistics: {statistics.RecordedIncrements} increment(s) recorded so far");
    }
}

// the handler is transient, so any state it accumulates must live in a service with a longer
// lifetime; here we use a singleton to keep the running total across all published signals
internal class CounterStatistics
{
    public int RecordedIncrements { get; set; }
}
