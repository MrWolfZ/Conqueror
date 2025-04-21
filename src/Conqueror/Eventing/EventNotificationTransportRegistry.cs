using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Eventing;

internal sealed class EventNotificationTransportRegistry(IEnumerable<EventNotificationHandlerRegistration> registrations)
    : IEventNotificationTransportRegistry
{
    private readonly List<EventNotificationHandlerRegistration> allRegistrations = registrations.ToList();
    private readonly ConcurrentDictionary<Type, List<IEventNotificationReceiverHandlerInvoker>> invokersByInjectorType = new();
    private readonly ConcurrentDictionary<Type, IEventNotificationTypesInjector?> typesInjectorsByNotificationType = new();

    public TTypesInjector? GetTypesInjectorForEventNotificationType<TTypesInjector>(Type eventNotificationType)
        where TTypesInjector : class, IEventNotificationTypesInjector
    {
        return (TTypesInjector?)typesInjectorsByNotificationType.GetOrAdd(eventNotificationType,
                                                                          t => allRegistrations.FirstOrDefault(r => r.EventNotificationType == t)?
                                                                                               .TypeInjectors
                                                                                               .OfType<TTypesInjector>()
                                                                                               .FirstOrDefault());
    }

    public IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker<TTypesInjector>> GetEventNotificationInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, IEventNotificationTypesInjector
    {
        var entries = invokersByInjectorType.GetOrAdd(typeof(TTypesInjector),
                                                      _ => [..PopulateEventNotificationInvokersForReceiver<TTypesInjector>()]);

        return entries.OfType<IEventNotificationReceiverHandlerInvoker<TTypesInjector>>().ToList();
    }

    private List<IEventNotificationReceiverHandlerInvoker> PopulateEventNotificationInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, IEventNotificationTypesInjector
    {
        var invokers = from r in allRegistrations
                       let typesInjector = r.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault()
                       where typesInjector is not null
                       select (IEventNotificationReceiverHandlerInvoker)new EventNotificationReceiverHandlerInvoker<TTypesInjector>(r, typesInjector);

        return invokers.ToList();
    }
}

internal sealed record EventNotificationHandlerRegistration(
    Type EventNotificationType,
    Type? HandlerType,
    Delegate? HandlerFn,
    IEventNotificationHandlerInvoker Invoker,
    IReadOnlyCollection<IEventNotificationTypesInjector> TypeInjectors);
