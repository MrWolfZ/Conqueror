using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public abstract class EventNotificationMiddlewareContext<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public abstract TEventNotification EventNotification { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract EventNotificationTransportType TransportType { get; }

    public abstract Task Next(TEventNotification notification, CancellationToken cancellationToken);
}
