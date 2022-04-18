using Conqueror.Eventing;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

internal sealed class PublishSharedCounterIncrementToHubEventObserver : IEventObserver<SharedCounterIncrementedEvent>
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
