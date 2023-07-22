using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventTransportClientDispatcher
{
    Task DispatchEvent(object evt, ISet<ConquerorEventObserverId> observersToDispatchTo, IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
