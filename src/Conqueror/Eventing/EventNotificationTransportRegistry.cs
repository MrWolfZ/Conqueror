using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationTransportRegistry : IEventNotificationTransportRegistry
{
    public TTypesInjector? GetTypesInjectorForEventNotificationType<TTypesInjector>(Type eventNotificationType)
        where TTypesInjector : class, IEventNotificationTypesInjector
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<(Type EventNotificationType, TTypesInjector TypesInjector, TReceiverConfiguration ReceiverConfiguration)>> GetEventNotificationTypesForReceiver<TTypesInjector, TReceiverConfiguration>()
        where TTypesInjector : class, IEventNotificationTypesInjector
        where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration
    {
        throw new NotImplementedException();
    }
}

public sealed record EventNotificationHandlerRegistration(
    Type EventNotificationType,
    Type? HandlerType,
    Delegate? HandlerFn,
    Delegate? ConfigurePipeline,
    IReadOnlyCollection<IEventNotificationTypesInjector> TypeInjectors);
