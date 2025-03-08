using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventObserverDispatcher<TEvent>(
    IServiceProvider serviceProvider,
    EventPublisherDispatcher dispatcher,
    Action<IEventPipeline<TEvent>>? configurePipeline = null)
    : IEventObserver<TEvent>
    where TEvent : class
{
    public Task Handle(TEvent evt, CancellationToken cancellationToken = default)
    {
        return dispatcher.DispatchEvent(evt, configurePipeline, serviceProvider, cancellationToken);
    }

    public IEventObserver<TEvent> WithPipeline(Action<IEventPipeline<TEvent>> configure)
    {
        var originalConfigure = configure;
        configure = pipeline =>
        {
            originalConfigure(pipeline);
            configurePipeline?.Invoke(pipeline);
        };

        return new EventObserverDispatcher<TEvent>(serviceProvider, dispatcher, configure);
    }
}
