using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal sealed class DelegateEventObserver<TEvent>(
    Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
    Action<IEventObserverPipelineBuilder>? configurePipeline,
    IServiceProvider serviceProvider,
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

        return pipeline.Execute(serviceProvider, observerFn, evt, observedEventType, cancellationToken);
    }
}
