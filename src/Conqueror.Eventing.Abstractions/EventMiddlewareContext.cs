using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class EventMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract TEvent Event { get; }

    /// <summary>
    ///     The type of event that the observer observes. This is useful when you need to run
    ///     different logic when a sub-type of the observed type is being handled. Note that when
    ///     a middleware runs in the context of a publish operation, this will always be the type of
    ///     the handled event.
    /// </summary>
    public abstract Type ObservedEventType { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract EventTransportType TransportType { get; }

    public abstract Task Next(TEvent evt, CancellationToken cancellationToken);
}
