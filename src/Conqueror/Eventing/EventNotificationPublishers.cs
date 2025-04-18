using System;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPublishers(IServiceProvider serviceProvider) : IEventNotificationPublishers
{
    public IEventNotificationHandler<TEventNotification> For<TEventNotification>(EventNotificationTypes<TEventNotification> notificationTypes)
        where TEventNotification : class, IEventNotification<TEventNotification>
        => For<TEventNotification>();

    public IEventNotificationHandler<TEventNotification> For<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return new EventNotificationHandlerProxy<TEventNotification>(serviceProvider,
                                                                     new(b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                                                                     null,
                                                                     EventNotificationTransportRole.Publisher);
    }
}
