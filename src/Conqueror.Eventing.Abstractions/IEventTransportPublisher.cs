using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IEventTransportPublisher<in TAttribute> : IEventTransportPublisher
    where TAttribute : EventTransportAttribute
{
    Task PublishEvent(object evt,
                      TAttribute attribute,
                      IServiceProvider serviceProvider,
                      CancellationToken cancellationToken);
}

public interface IEventTransportPublisher;
