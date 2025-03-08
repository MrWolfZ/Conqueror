using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal sealed class EventObserverInvoker<TEvent>(
    Action<IEventPipeline<TEvent>>? configurePipeline,
    Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
    Type? observerType)
    : IEventObserverInvoker
    where TEvent : class
{
    public Type EventType { get; } = typeof(TEvent);

    public Type? ObserverType { get; } = observerType;

    public bool AcceptsEventType(Type eventType) => eventType.IsAssignableTo(typeof(TEvent));

    public Task Invoke(IServiceProvider serviceProvider, EventTransportAttribute attribute, object evt, CancellationToken cancellationToken)
    {
        return EventPipelineInvoker.RunPipeline((TEvent)evt,
                                                configurePipeline,
                                                attribute,
                                                serviceProvider,
                                                EventTransportRole.Receiver,
                                                observerFn,
                                                cancellationToken);
    }
}

internal interface IEventObserverInvoker
{
    Type EventType { get; }

    Type? ObserverType { get; }

    bool AcceptsEventType(Type eventType);

    Task Invoke(
        IServiceProvider serviceProvider,
        EventTransportAttribute attribute,
        object evt,
        CancellationToken cancellationToken);
}
