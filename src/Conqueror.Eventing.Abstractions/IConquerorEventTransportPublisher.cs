using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportPublisher<in TConfigurationAttribute> : IConquerorEventTransportPublisher
    where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
{
    Task PublishEvent<TEvent>(TEvent evt, TConfigurationAttribute configurationAttribute, CancellationToken cancellationToken = default)
        where TEvent : class;
}

public interface IConquerorEventTransportPublisher;
