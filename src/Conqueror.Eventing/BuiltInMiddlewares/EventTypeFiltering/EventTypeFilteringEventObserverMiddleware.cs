using System.Threading.Tasks;

namespace Conqueror.Eventing.BuiltInMiddlewares.EventTypeFiltering;

internal sealed class EventTypeFilteringEventObserverMiddleware : IEventObserverMiddleware<EventTypeFilteringEventObserverMiddlewareConfiguration>
{
    public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, EventTypeFilteringEventObserverMiddlewareConfiguration> ctx)
        where TEvent : class
    {
        if (!ctx.Configuration.AllowInvocationWithEventSubType && ctx.ObservedEventType != typeof(TEvent))
        {
            return Task.CompletedTask;
        }

        return ctx.Next(ctx.Event, ctx.CancellationToken);
    }
}
