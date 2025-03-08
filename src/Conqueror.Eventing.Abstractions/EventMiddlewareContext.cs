using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class EventMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract TEvent Event { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract EventTransportType TransportType { get; }

    public abstract Task Next(TEvent evt, CancellationToken cancellationToken);
}
