using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventObserverProxy<TEvent>(
    IServiceProvider serviceProvider,
    Action<IEventObserverPipelineBuilder>? configurePipeline,
    Type observerType,
    Type observedEventType)
    : IEventObserver<TEvent>
    where TEvent : class
{
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
