using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportReceiverRegistry
{
    Task<ConquerorEventTransportReceiverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>> RegisterReceiver<TObserverTransportConfiguration, TConfigurationAttribute>(
        Action<IConquerorEventBroadcastingStrategyBuilder> broadcastingStrategyConfiguration)
        where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
        where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;
}

public sealed record ConquerorEventTransportReceiverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>(
    IConquerorEventTransportReceiverDispatcher Dispatcher,
    IReadOnlyCollection<ConquerorEventTransportReceiverObserverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>> RelevantObservers)
    where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
    where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;

public sealed record ConquerorEventObserverId(Guid Id);

public sealed record ConquerorEventTransportReceiverObserverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>(
    ConquerorEventObserverId ObserverId,
    Type EventType,
    TConfigurationAttribute ConfigurationAttribute,
    TObserverTransportConfiguration? Configuration)
    where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
    where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;
