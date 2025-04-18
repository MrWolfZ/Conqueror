using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate Task EventNotificationHandlerFn(object evt, CancellationToken cancellationToken);

public interface IEventNotificationBroadcastingStrategy
{
    Task BroadcastEvent(IReadOnlyCollection<EventNotificationHandlerFn> eventNotificationHandlers,
                        IServiceProvider serviceProvider,
                        object notification,
                        CancellationToken cancellationToken);
}
