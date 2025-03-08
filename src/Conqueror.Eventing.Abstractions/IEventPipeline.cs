using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror;

public delegate Task EventMiddlewareFn<TEvent>(EventMiddlewareContext<TEvent> context)
    where TEvent : class;

public interface IEventPipeline<TEvent> : IReadOnlyCollection<IEventMiddleware<TEvent>>
    where TEvent : class
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     The transport type this pipeline is being built for. This property can be useful
    ///     to build pipeline extension methods that should include certain middlewares only
    ///     for specific transports (e.g. including a logging middleware only if the transport
    ///     is not the default in-process transport to prevent duplicate log entries).
    /// </summary>
    EventTransportType TransportType { get; }

    IEventPipeline<TEvent> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IEventMiddleware<TEvent>;

    IEventPipeline<TEvent> Use(EventMiddlewareFn<TEvent> middlewareFn);

    IEventPipeline<TEvent> Without<TMiddleware>()
        where TMiddleware : IEventMiddleware<TEvent>;

    IEventPipeline<TEvent> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IEventMiddleware<TEvent>;
}
