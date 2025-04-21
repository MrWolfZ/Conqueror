using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationPublishers
{
    THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TEventNotification,
        THandler>()
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>;

    THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TEventNotification,
        THandler>(EventNotificationTypes<TEventNotification, THandler> notificationTypes)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>;
}
