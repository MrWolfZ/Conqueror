using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationTransportRegistry
{
    TTypesInjector? GetTypesInjectorForEventNotificationType<TTypesInjector>(Type eventNotificationType)
        where TTypesInjector : class, IEventNotificationTypesInjector;

    Task<IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration>>> GetEventNotificationInvokersForReceiver<TTypesInjector, TReceiverConfiguration>(
        CancellationToken cancellationToken)
        where TTypesInjector : class, IEventNotificationTypesInjector
        where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration;
}
