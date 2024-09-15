using System;

namespace Conqueror;

public interface ICommandPipeline<TCommand> : ICommandPipeline<TCommand, UnitCommandResponse>
    where TCommand : class;

public interface ICommandPipeline<TCommand, TResponse>
    where TCommand : class
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     The transport type this pipeline is being built for. This property can be useful
    ///     to build pipeline extension methods that should include certain middlewares only
    ///     for specific transports (e.g. including a logging middleware only if the transport
    ///     is not the default in-process transport to prevent duplicate log entries).
    /// </summary>
    CommandTransportType TransportType { get; }

    ICommandPipeline<TCommand, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>;

    ICommandPipeline<TCommand, TResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>;

    ICommandPipeline<TCommand, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>;
}
