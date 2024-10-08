namespace Conqueror.Examples.BlazorWebAssembly.Tests;

public sealed class TestEventHub : IEventHub
{
    private readonly List<object> observedEvents = [];

    public IReadOnlyCollection<object> ObservedEvents => observedEvents;

    public Task PublishEvent<TEvent>(TEvent evt)
        where TEvent : class
    {
        observedEvents.Add(evt);
        return Task.CompletedTask;
    }
}
