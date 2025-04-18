using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal delegate Task EventNotificationMiddlewareNext<in TEventNotification>(TEventNotification notification, CancellationToken cancellationToken);

internal sealed class DefaultEventNotificationMiddlewareContext<TEventNotification>(
    TEventNotification notification,
    EventNotificationMiddlewareNext<TEventNotification> next,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    EventNotificationTransportType transportType,
    CancellationToken cancellationToken)
    : EventNotificationMiddlewareContext<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public override TEventNotification EventNotification { get; } = notification;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override EventNotificationTransportType TransportType { get; } = transportType;

    public override Task Next(TEventNotification notification, CancellationToken cancellationToken) => next(notification, cancellationToken);
}
