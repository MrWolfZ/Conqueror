namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounters;

internal sealed class PublishSharedCounterIncrementToHubEventObserver(IEventHub eventHub) : ISharedCounterIncrementedEventObserver
{
    public async Task Handle(SharedCounterIncrementedEvent evt, CancellationToken cancellationToken = default)
    {
        await eventHub.PublishEvent(evt);
    }
}
