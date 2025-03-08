namespace Conqueror.Examples.BlazorWebAssembly.Application;

internal sealed class InMemoryEventStore : ISharedCounterIncrementedEventObserver
{
    private readonly List<object> events = [];

    public Task Handle(SharedCounterIncrementedEvent evt, CancellationToken cancellationToken = default)
    {
        events.Add(evt);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<object> GetEvents() => events;
}
