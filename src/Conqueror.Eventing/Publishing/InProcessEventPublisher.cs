using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Eventing.Observing;

namespace Conqueror.Eventing.Publishing;

internal sealed class InProcessEventPublisher(InProcessEventTransportReceiver receiver) : IEventTransportPublisher<InProcessEventAttribute>
{
    public Task PublishEvent(object evt, InProcessEventAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return receiver.Handle(evt, serviceProvider, cancellationToken);
    }
}
