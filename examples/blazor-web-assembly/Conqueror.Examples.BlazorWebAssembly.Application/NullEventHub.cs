namespace Conqueror.Examples.BlazorWebAssembly.Application;

// in a real application this would publish to an Azure Event Hub or similar
internal sealed class NullEventHub : IEventHub
{
    public Task PublishEvent<TEvent>(TEvent evt)
        where TEvent : class
    {
        // nothing to do
        return Task.CompletedTask;
    }
}
