using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IEventTransportReceiverBroadcaster
{
    Task Broadcast(object evt,
                   EventTransportAttribute attribute,
                   IServiceProvider serviceProvider,
                   CancellationToken cancellationToken);
}
