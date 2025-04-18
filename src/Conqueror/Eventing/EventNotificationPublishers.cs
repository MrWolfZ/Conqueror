using System;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPublishers(IServiceProvider serviceProvider) : IEventNotificationPublishers
{
    public IEventNotificationHandler<TEventNotification> For<TEventNotification>(EventNotificationTypes<TEventNotification> notificationTypes)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        _ = serviceProvider;
        throw new NotImplementedException();
    }

    public IEventNotificationHandler<TEventNotification> For<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        _ = serviceProvider;
        throw new NotImplementedException();
    }
}
