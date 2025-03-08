using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Eventing.Observing;

namespace Conqueror.Eventing.Publishing;

internal sealed class InProcessEventPublisher(InProcessEventTransportReceiver receiver) : IConquerorEventTransportPublisher<InProcessEventAttribute>
{
    public string TransportTypeName => InProcessEventTransportTypeExtensions.TransportName;

    public Task PublishEvent<TEvent>(TEvent evt, InProcessEventAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return receiver.Handle(evt, serviceProvider, cancellationToken);
    }
}
