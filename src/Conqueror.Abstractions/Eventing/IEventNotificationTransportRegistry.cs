using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationTransportRegistry
{
    TTypesInjector? GetTypesInjectorForEventNotificationType<TTypesInjector>(Type eventNotificationType)
        where TTypesInjector : class, IEventNotificationTypesInjector;

    IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker<TTypesInjector>> GetEventNotificationInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, IEventNotificationTypesInjector;
}
