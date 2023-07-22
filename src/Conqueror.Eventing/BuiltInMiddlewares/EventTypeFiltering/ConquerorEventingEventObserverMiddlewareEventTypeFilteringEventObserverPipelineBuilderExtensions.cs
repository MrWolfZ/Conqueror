using Conqueror.Eventing.BuiltInMiddlewares.EventTypeFiltering;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class ConquerorEventingEventObserverMiddlewareEventTypeFilteringEventObserverPipelineBuilderExtensions
{
    /// <summary>
    ///     Add event type filtering functionality to an event observer pipeline. This is mainly useful for disallowing
    ///     invocation of the observer with sub-types of the event types it is observing (which is allowed by default).
    /// </summary>
    /// <param name="pipeline">The event observer pipeline to add event type filtering to</param>
    /// <returns>The event observer pipeline</returns>
    public static IEventObserverPipelineBuilder UseEventTypeFiltering(this IEventObserverPipelineBuilder pipeline)
    {
        return pipeline.Use<EventTypeFilteringEventObserverMiddleware, EventTypeFilteringEventObserverMiddlewareConfiguration>(new());
    }

    /// <summary>
    ///     Disallow invocation of the observer with sub-types of the event types it is observing. If this middleware is
    ///     called with a sub-type, the pipeline returns immediately and no further downstream middlewares are called.
    /// </summary>
    /// <param name="pipeline">The event observer pipeline to configure the event type filtering middleware in</param>
    /// <returns>The event observer pipeline</returns>
    public static IEventObserverPipelineBuilder DisallowInvocationWithSubTypes(this IEventObserverPipelineBuilder pipeline)
    {
        return pipeline.Configure<EventTypeFilteringEventObserverMiddleware, EventTypeFilteringEventObserverMiddlewareConfiguration>(o =>
        {
            // comment only for formatting purposes in order to prevent this line being inlined above
            o.AllowInvocationWithEventSubType = false;
        });
    }

    /// <summary>
    ///     Remove the event type filtering middleware from an event observer pipeline.
    /// </summary>
    /// <param name="pipeline">The event observer pipeline with the event type filtering middleware to remove</param>
    /// <returns>The event observer pipeline</returns>
    public static IEventObserverPipelineBuilder WithoutEventTypeFiltering(this IEventObserverPipelineBuilder pipeline)
    {
        return pipeline.Without<EventTypeFilteringEventObserverMiddleware, EventTypeFilteringEventObserverMiddlewareConfiguration>();
    }
}
