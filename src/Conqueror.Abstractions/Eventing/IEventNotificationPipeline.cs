using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CA1034

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate Task EventNotificationMiddlewareFn<TEventNotification>(EventNotificationMiddlewareContext<TEventNotification> context)
    where TEventNotification : class, IEventNotification<TEventNotification>;

public interface IEventNotificationPipeline<TEventNotification> : IReadOnlyCollection<IEventNotificationMiddleware<TEventNotification>>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     The transport type this pipeline is being built for. This property can be useful
    ///     to build pipeline extension methods that should include certain middlewares only
    ///     for specific transports (e.g. including a logging middleware only if the transport
    ///     is not the default in-process transport to prevent duplicate log entries).
    /// </summary>
    EventNotificationTransportType TransportType { get; }

    IEventNotificationPipeline<TEventNotification> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IEventNotificationMiddleware<TEventNotification>;

    IEventNotificationPipeline<TEventNotification> Use(EventNotificationMiddlewareFn<TEventNotification> middlewareFn);

    IEventNotificationPipeline<TEventNotification> Without<TMiddleware>()
        where TMiddleware : IEventNotificationMiddleware<TEventNotification>;

    IEventNotificationPipeline<TEventNotification> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IEventNotificationMiddleware<TEventNotification>;
}
