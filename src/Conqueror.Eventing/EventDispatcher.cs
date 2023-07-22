using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventDispatcher : IConquerorEventDispatcher
{
    private readonly EventPublisherDispatcher publisherDispatcher;
    private readonly IServiceProvider serviceProvider;

    public EventDispatcher(IServiceProvider serviceProvider, EventPublisherDispatcher publisherDispatcher)
    {
        this.serviceProvider = serviceProvider;
        this.publisherDispatcher = publisherDispatcher;
    }

    public Task DispatchEvent<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return publisherDispatcher.DispatchEvent(evt, serviceProvider, cancellationToken);
    }
}
