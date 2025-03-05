using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal sealed class InProcessEventTransportReceiver(IConquerorEventTypeRegistry registry, IConquerorEventTransportReceiverBroadcaster broadcaster)
{
    public async Task Handle<TEvent>(TEvent evt, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (registry.TryGetConfigurationForReceiver<InMemoryEventAttribute>(evt.GetType(), out _))
        {
            await broadcaster.Broadcast(evt, serviceProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
