using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventDispatcher(
    IServiceProvider serviceProvider,
    EventPublisherDispatcher publisherDispatcher)
    : IConquerorEventDispatcher
{
    public Task DispatchEvent<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return publisherDispatcher.DispatchEvent(evt, serviceProvider, cancellationToken);
    }
}
