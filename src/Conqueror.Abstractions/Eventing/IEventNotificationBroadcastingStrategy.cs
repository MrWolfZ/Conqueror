using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationBroadcastingStrategy
{
    Task BroadcastEventNotification<TEventNotification>(IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> eventNotificationHandlerInvokers,
                                                        IServiceProvider serviceProvider,
                                                        TEventNotification notification,
                                                        string transportTypeName,
                                                        CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>;
}
