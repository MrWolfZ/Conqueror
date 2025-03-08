using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public delegate Task EventObserverFn(object evt, CancellationToken cancellationToken);

public interface IEventBroadcastingStrategy
{
    Task BroadcastEvent(IReadOnlyCollection<EventObserverFn> eventObservers,
                        IServiceProvider serviceProvider,
                        object evt,
                        CancellationToken cancellationToken);
}
