namespace Conqueror.Eventing.BuiltInMiddlewares.EventTypeFiltering;

public sealed class EventTypeFilteringEventObserverMiddlewareConfiguration
{
    /// <summary>
    ///     Determine whether the event observer should be called with sub-types of the
    ///     event types it is observing. Defaults to <c>true</c>.
    /// </summary>
    public bool AllowInvocationWithEventSubType { get; set; } = true;
}
