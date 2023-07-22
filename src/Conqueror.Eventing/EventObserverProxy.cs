using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventObserverProxy<TEvent> : IEventObserver<TEvent>
    where TEvent : class
{
    private readonly Action<IEventObserverPipelineBuilder>? configurePipeline;
    private readonly Type observedEventType;
    private readonly Type observerType;
    private readonly IServiceProvider serviceProvider;

    public EventObserverProxy(IServiceProvider serviceProvider,
                              Action<IEventObserverPipelineBuilder>? configurePipeline,
                              Type observerType,
                              Type observedEventType)
    {
        this.serviceProvider = serviceProvider;
        this.configurePipeline = configurePipeline;
        this.observerType = observerType;
        this.observedEventType = observedEventType;
    }

    public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default)
    {
        var pipelineBuilder = new EventObserverPipelineBuilder(serviceProvider);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build();

        // TODO: add context support
        // var pipeline = pipelineBuilder.Build(conquerorContext);

        return pipeline.Execute(serviceProvider, observerType, evt, observedEventType, cancellationToken);
    }
}
