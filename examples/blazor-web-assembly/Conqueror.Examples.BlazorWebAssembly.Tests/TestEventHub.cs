namespace Conqueror.Examples.BlazorWebAssembly.API.Tests;

public sealed class TestEventHub : IEventHub
{
    private readonly List<object> observedEvents = new();

    public IReadOnlyCollection<object> ObservedEvents => observedEvents;

    public Task PublishEvent<TEvent>(TEvent evt)
        where TEvent : class
    {
        observedEvents.Add(evt);
        return Task.CompletedTask;
    }
}
