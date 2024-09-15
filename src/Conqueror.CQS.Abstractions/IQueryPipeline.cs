using System;

namespace Conqueror;

public interface IQueryPipeline<TQuery, TResponse>
    where TQuery : class
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     The transport type this pipeline is being built for. This property can be useful
    ///     to build pipeline extension methods that should include certain middlewares only
    ///     for specific transports (e.g. including a logging middleware only if the transport
    ///     is not the default in-process transport to prevent duplicate log entries).
    /// </summary>
    QueryTransportType TransportType { get; }

    IQueryPipeline<TQuery, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>;

    IQueryPipeline<TQuery, TResponse> Without<TMiddleware>()
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>;

    IQueryPipeline<TQuery, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>;
}
