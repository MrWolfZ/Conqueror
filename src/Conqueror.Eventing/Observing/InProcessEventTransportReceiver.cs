using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal sealed class InProcessEventTransportReceiver(IEventTransportReceiverBroadcaster broadcaster)
{
    public async Task Handle<TEvent>(TEvent evt, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        where TEvent : class
    {
        await broadcaster.Broadcast(evt, new InProcessEventAttribute(), serviceProvider, cancellationToken).ConfigureAwait(false);
    }
}
