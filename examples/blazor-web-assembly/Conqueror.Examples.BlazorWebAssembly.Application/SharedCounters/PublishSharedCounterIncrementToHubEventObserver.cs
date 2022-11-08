namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounters;

internal sealed class PublishSharedCounterIncrementToHubEventObserver : ISharedCounterIncrementedEventObserver
{
    private readonly IEventHub eventHub;

    public PublishSharedCounterIncrementToHubEventObserver(IEventHub eventHub)
    {
        this.eventHub = eventHub;
    }

    public async Task HandleEvent(SharedCounterIncrementedEvent evt, CancellationToken cancellationToken)
    {
        await eventHub.PublishEvent(evt);
    }
}
