using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationTransportRegistry
{
    TTypesInjector? GetTypesInjectorForEventNotificationType<TTypesInjector>(Type eventNotificationType)
        where TTypesInjector : class, IEventNotificationTypesInjector;

    Task<IReadOnlyCollection<(Type EventNotificationType, TTypesInjector TypesInjector, TReceiverConfiguration ReceiverConfiguration)>> GetEventNotificationTypesForReceiver<TTypesInjector, TReceiverConfiguration>()
        where TTypesInjector : class, IEventNotificationTypesInjector
        where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration;
}
