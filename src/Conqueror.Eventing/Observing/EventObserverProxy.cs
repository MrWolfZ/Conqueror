using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal sealed class EventObserverProxy<TEvent>(
    IServiceProvider serviceProvider,
    Action<IEventPipeline<TEvent>>? configurePipeline,
    Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
    Type observedEventType,
    ConquerorEventTransportAttribute configurationAttribute)
    : IEventObserver<TEvent>
    where TEvent : class
{
    public Task Handle(TEvent evt, CancellationToken cancellationToken = default)
    {
        return EventPipelineInvoker.RunPipeline(evt,
                                                configurePipeline,
                                                configurationAttribute,
                                                serviceProvider,
                                                observedEventType,
                                                EventTransportRole.Receiver,
                                                observerFn,
                                                cancellationToken);
    }
}

internal sealed class EventObserverProxy<TEvent, TObservedEvent>(
    IServiceProvider serviceProvider,
    Action<IEventPipeline<TObservedEvent>>? configurePipeline,
    Func<TObservedEvent, IServiceProvider, CancellationToken, Task> observerFn,
    Type observedEventType,
    ConquerorEventTransportAttribute configurationAttribute)
    : IEventObserver<TEvent>
    where TEvent : class, TObservedEvent
    where TObservedEvent : class
{
    public Task Handle(TEvent evt, CancellationToken cancellationToken = default)
    {
        return EventPipelineInvoker.RunPipeline(evt,
                                                configurePipeline,
                                                configurationAttribute,
                                                serviceProvider,
                                                observedEventType,
                                                EventTransportRole.Receiver,
                                                observerFn,
                                                cancellationToken);
    }
}
