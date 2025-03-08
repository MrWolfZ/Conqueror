using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventDispatcher(IServiceProvider serviceProvider, EventPublisherDispatcher publisherDispatcher) : IEventDispatcher
{
    public Task DispatchEvent<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return publisherDispatcher.DispatchEvent(evt, null, serviceProvider, cancellationToken);
    }

    public Task DispatchEvent<TEvent>(TEvent evt, Action<IEventPipeline<TEvent>> configurePipeline, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return publisherDispatcher.DispatchEvent(evt, configurePipeline, serviceProvider, cancellationToken);
    }
}
