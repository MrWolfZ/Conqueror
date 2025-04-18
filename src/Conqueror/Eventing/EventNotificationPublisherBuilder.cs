using System;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPublisherBuilder<TEventNotification>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext)
    : IEventNotificationPublisherBuilder<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;
}
