using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate Task EventNotificationHandlerFn<in TEventNotification>(TEventNotification notification,
                                                                       IServiceProvider serviceProvider,
                                                                       CancellationToken cancellationToken)
    where TEventNotification : class, IEventNotification<TEventNotification>;

public delegate void EventNotificationHandlerSyncFn<in TEventNotification>(TEventNotification notification,
                                                                           IServiceProvider serviceProvider,
                                                                           CancellationToken cancellationToken)
    where TEventNotification : class, IEventNotification<TEventNotification>;
