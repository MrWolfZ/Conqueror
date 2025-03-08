using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IConquerorEventBroadcastingStrategy
{
    Task BroadcastEvent<TEvent>(IReadOnlyCollection<IEventObserver<TEvent>> eventObservers,
                                IServiceProvider serviceProvider,
                                TEvent evt,
                                CancellationToken cancellationToken)
        where TEvent : class;
}
