using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportReceiverBroadcaster
{
    Task Broadcast(object evt, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
