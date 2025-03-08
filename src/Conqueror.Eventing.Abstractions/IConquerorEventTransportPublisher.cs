using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportPublisher<in TConfigurationAttribute> : IConquerorEventTransportPublisher
    where TConfigurationAttribute : ConquerorEventTransportAttribute
{
    Task PublishEvent<TEvent>(TEvent evt,
                              TConfigurationAttribute configurationAttribute,
                              IServiceProvider serviceProvider,
                              CancellationToken cancellationToken = default)
        where TEvent : class;
}

public interface IConquerorEventTransportPublisher;
