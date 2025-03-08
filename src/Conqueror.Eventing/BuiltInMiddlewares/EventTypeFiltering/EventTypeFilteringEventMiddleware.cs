using System.Threading.Tasks;

namespace Conqueror.Eventing.BuiltInMiddlewares.EventTypeFiltering;

internal sealed class EventTypeFilteringEventMiddleware<TEvent> : IEventMiddleware<TEvent>
    where TEvent : class
{
    /// <summary>
    ///     Determine whether the event observer should be called with sub-types of the
    ///     event types it is observing. Defaults to <c>true</c>.
    /// </summary>
    public bool AllowInvocationWithEventSubType { get; set; } = true;

    public Task Execute(EventMiddlewareContext<TEvent> ctx)
    {
        if (!AllowInvocationWithEventSubType && ctx.Event.GetType() != typeof(TEvent))
        {
            return Task.CompletedTask;
        }

        return ctx.Next(ctx.Event, ctx.CancellationToken);
    }
}
