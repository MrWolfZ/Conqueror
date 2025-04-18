// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationPublishers
{
    IEventNotificationHandler<TEventNotification> For<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>;

    IEventNotificationHandler<TEventNotification> For<TEventNotification>(EventNotificationTypes<TEventNotification> notificationTypes)
        where TEventNotification : class, IEventNotification<TEventNotification>;
}
