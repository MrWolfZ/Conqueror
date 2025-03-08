using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportReceiverBroadcaster
{
    Task Broadcast(object evt,
                   ConquerorEventTransportAttribute configurationAttribute,
                   IServiceProvider serviceProvider,
                   CancellationToken cancellationToken);
}
