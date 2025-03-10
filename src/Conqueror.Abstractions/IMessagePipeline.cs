using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror;

public delegate Task<TResponse> MessageMiddlewareFn<TMessage, TResponse>(MessageMiddlewareContext<TMessage, TResponse> context)
    where TMessage : class, IMessage<TResponse>;

public interface IMessagePipeline<TMessage> : IMessagePipeline<TMessage, UnitMessageResponse>
    where TMessage : class, IMessage<UnitMessageResponse>;

public interface IMessagePipeline<TMessage, TResponse> : IReadOnlyCollection<IMessageMiddleware<TMessage, TResponse>>
    where TMessage : class, IMessage<TResponse>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     The transport type this pipeline is being built for. This property can be useful
    ///     to build pipeline extension methods that should include certain middlewares only
    ///     for specific transports (e.g. including a logging middleware only if the transport
    ///     is not the default in-process transport to prevent duplicate log entries).
    /// </summary>
    MessageTransportType TransportType { get; }

    IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;

    IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn);

    IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;

    IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;
}
