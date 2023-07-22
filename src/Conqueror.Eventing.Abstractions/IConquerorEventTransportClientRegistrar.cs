using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportClientRegistrar
{
    Task<ConquerorEventTransportClientRegistration<TObserverTransportConfiguration, TConfigurationAttribute>> RegisterTransportClient<TObserverTransportConfiguration, TConfigurationAttribute>(
        Action<IConquerorInMemoryEventPublishingStrategyBuilder> inMemoryPublishingStrategyConfiguration)
        where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
        where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;
}

public sealed record ConquerorEventTransportClientRegistration<TObserverTransportConfiguration, TConfigurationAttribute>(
    IConquerorEventTransportClientDispatcher Dispatcher,
    IReadOnlyCollection<ConquerorEventTransportClientObserverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>> RelevantObservers)
    where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
    where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;

public sealed record ConquerorEventObserverId(Guid Id);

public sealed record ConquerorEventTransportClientObserverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>(
    ConquerorEventObserverId ObserverId,
    Type EventType,
    TConfigurationAttribute ConfigurationAttribute,
    TObserverTransportConfiguration? Configuration)
    where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
    where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;
