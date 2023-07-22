using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class DelegateEventObserver<TEvent> : IEventObserver<TEvent>
    where TEvent : class
{
    private readonly Action<IEventObserverPipelineBuilder>? configurePipeline;
    private readonly Type observedEventType;
    private readonly Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn;
    private readonly IServiceProvider serviceProvider;

    public DelegateEventObserver(Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                 Action<IEventObserverPipelineBuilder>? configurePipeline,
                                 IServiceProvider serviceProvider,
                                 Type observedEventType)
    {
        this.observerFn = observerFn;
        this.configurePipeline = configurePipeline;
        this.serviceProvider = serviceProvider;
        this.observedEventType = observedEventType;
    }

    public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default)
    {
        var pipelineBuilder = new EventObserverPipelineBuilder(serviceProvider);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build();

        // TODO: add context support
        // var pipeline = pipelineBuilder.Build(conquerorContext);

        return pipeline.Execute(serviceProvider, observerFn, evt, observedEventType, cancellationToken);
    }
}
