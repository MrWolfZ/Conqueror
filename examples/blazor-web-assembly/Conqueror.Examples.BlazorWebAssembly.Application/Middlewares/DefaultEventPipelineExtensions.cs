using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public static class DefaultEventPipelineExtensions
{
    public static IEventPipeline<TEvent> UseDefault<TEvent>(this IEventPipeline<TEvent> pipeline)
        where TEvent : class
    {
        return pipeline.UseLogging();
    }

    public static IEventObserver<TEvent> WithDefaultPublisherPipeline<TEvent>(this IEventObserver<TEvent> observer)
        where TEvent : class
    {
        return observer.WithPipeline(p => p.UseDefault());
    }

    public static Task DispatchEventWithDefaultPublisherPipeline<TEvent>(this IEventDispatcher dispatcher,
                                                                         TEvent evt,
                                                                         CancellationToken cancellationToken)
        where TEvent : class
    {
        return dispatcher.DispatchEvent(evt, p => p.UseDefault(), cancellationToken);
    }
}
