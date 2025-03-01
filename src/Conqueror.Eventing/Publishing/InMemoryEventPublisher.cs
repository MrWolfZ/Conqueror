using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Eventing.Observing;

namespace Conqueror.Eventing.Publishing;

/// <summary>
///     This class acts both as a publisher and transport client for in-memory events.
/// </summary>
internal sealed class InMemoryEventPublisher(InProcessEventTransportReceiver receiver, IServiceProvider serviceProvider)
    : IConquerorEventTransportPublisher<InMemoryEventAttribute>
{
    public Task PublishEvent<TEvent>(TEvent evt, InMemoryEventAttribute configurationAttribute, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return receiver.Handle(evt, serviceProvider, cancellationToken);
    }
}
