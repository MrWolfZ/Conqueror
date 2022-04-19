namespace Conqueror.Examples.BlazorWebAssembly.Application;

internal sealed class InMemoryEventStore : ISharedCounterIncrementedEventObserver
{
    private readonly List<object> events = new();

    public Task HandleEvent(SharedCounterIncrementedEvent evt, CancellationToken cancellationToken)
    {
        events.Add(evt);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<object> GetEvents() => events;
}
