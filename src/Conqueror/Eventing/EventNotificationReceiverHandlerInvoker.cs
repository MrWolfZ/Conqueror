using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationReceiverHandlerInvoker<TTypesInjector>(
    EventNotificationHandlerRegistration registration,
    TTypesInjector typesInjector)
    : IEventNotificationReceiverHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, IEventNotificationTypesInjector
{
    public Type EventNotificationType { get; } = registration.EventNotificationType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public Task Invoke<TEventNotification>(IServiceProvider serviceProvider,
                                           TEventNotification notification,
                                           string transportTypeName,
                                           CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return registration.Invoker.Invoke(serviceProvider, notification, transportTypeName, cancellationToken);
    }
}
