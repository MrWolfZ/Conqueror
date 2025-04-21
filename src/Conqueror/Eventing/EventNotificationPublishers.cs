using System;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPublishers(IServiceProvider serviceProvider) : IEventNotificationPublishers
{
    public THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TEventNotification,
        THandler>()
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
    {
        return TEventNotification.DefaultTypeInjector.CreateWithEventNotificationTypes(new Injectable<THandler>(serviceProvider));
    }

    public THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TEventNotification,
        THandler>(EventNotificationTypes<TEventNotification, THandler> notificationTypes)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
    {
        return TEventNotification.DefaultTypeInjector.CreateWithEventNotificationTypes(new Injectable<THandler>(serviceProvider));
    }

    private sealed class Injectable<THandlerParam>(IServiceProvider serviceProvider) : IDefaultEventNotificationTypesInjectable<THandlerParam>
        where THandlerParam : class
    {
        THandlerParam IDefaultEventNotificationTypesInjectable<THandlerParam>
            .WithInjectedTypes<
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                TEventNotification,
                TGeneratedHandlerInterface,
                TGeneratedHandlerAdapter>()
        {
            var dispatcher = new EventNotificationDispatcher<TEventNotification>(serviceProvider,
                                                                                 new(b => b.UseInProcessWithSequentialBroadcastingStrategy()),
                                                                                 null,
                                                                                 EventNotificationTransportRole.Publisher);

            var adapter = new TGeneratedHandlerAdapter
            {
                Dispatcher = dispatcher,
            };

            return adapter as THandlerParam ?? throw new InvalidOperationException("could not create handler adapter");
        }
    }
}
