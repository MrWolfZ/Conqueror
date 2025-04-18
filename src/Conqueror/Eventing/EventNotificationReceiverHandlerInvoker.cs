using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration>(
    IReadOnlyCollection<EventNotificationHandlerInvokerForEventNotificationType<TTypesInjector, TReceiverConfiguration>> invokersForEventNotificationTypes)
    : IEventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration>
    where TTypesInjector : class, IEventNotificationTypesInjector
    where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<EventNotificationHandlerInvokerForEventNotificationType<TTypesInjector, TReceiverConfiguration>>> invokersByNotificationType = new();

    public IReadOnlyCollection<(TTypesInjector TypesInjector, TReceiverConfiguration ReceiverConfiguration)> HandledEventNotificationTypes { get; }
        = invokersForEventNotificationTypes.Select(i => (i.TypesInjector, i.ReceiverConfiguration)).ToList();

    public bool AcceptsEventNotificationType(Type eventNotificationType) => invokersForEventNotificationTypes.Any(i => i.AcceptsEventNotificationType(eventNotificationType));

    public async Task Invoke<TEventNotification>(IServiceProvider serviceProvider,
                                                 TEventNotification notification,
                                                 string transportTypeName,
                                                 CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        // 99% of the time, a handler will only have a single invoker for a given event notification type, but there is
        // an edge case where a handler may support multiple event notification types from the same type hierarchy and
        // in those cases we need to call the handler multiple times; even if that is not the case, for handlers that
        // observe a lot of event notification types, we want to avoid having to iterate over the list of invokers every
        // time, so we cache the list of invokers for each event notification type
        var relevantInvokers = invokersByNotificationType.GetOrAdd(
            notification.GetType(), t => invokersForEventNotificationTypes.Where(i => i.AcceptsEventNotificationType(t)).ToList());

        foreach (var invoker in relevantInvokers)
        {
            await invoker.Invoke(serviceProvider, notification, transportTypeName, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed class EventNotificationHandlerInvokerForEventNotificationType<TTypesInjector, TReceiverConfiguration>(
    EventNotificationHandlerRegistration registration,
    TTypesInjector typesInjector,
    TReceiverConfiguration receiverConfiguration)
    where TTypesInjector : class, IEventNotificationTypesInjector
    where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration
{
    public TTypesInjector TypesInjector { get; } = typesInjector;

    public TReceiverConfiguration ReceiverConfiguration { get; } = receiverConfiguration;

    public bool AcceptsEventNotificationType(Type eventNotificationType) => eventNotificationType.IsAssignableTo(registration.EventNotificationType);

    public Task Invoke<TEventNotification>(IServiceProvider serviceProvider,
                                           TEventNotification notification,
                                           string transportTypeName,
                                           CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return registration.Invoker.Invoke(serviceProvider, notification, transportTypeName, cancellationToken);
    }
}
