namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounters;

internal sealed class PublishSharedCounterIncrementToHubEventObserver(IEventHub eventHub) : ISharedCounterIncrementedEventObserver
{
    public async Task HandleEvent(SharedCounterIncrementedEvent evt, CancellationToken cancellationToken = default)
    {
        await eventHub.PublishEvent(evt);
    }
}
