using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

#pragma warning disable CA1034

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate Task<TResponse> MessageMiddlewareFn<TMessage, TResponse>(MessageMiddlewareContext<TMessage, TResponse> context)
    where TMessage : class, IMessage<TMessage, TResponse>;

public interface IMessagePipeline<TMessage, TResponse> : IReadOnlyCollection<IMessageMiddleware<TMessage, TResponse>>
    where TMessage : class, IMessage<TMessage, TResponse>
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

[EditorBrowsable(EditorBrowsableState.Never)]
public class GeneratedMessagePipelineAdapter<TMessage, TResponse>
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public IMessagePipeline<TMessage, TResponse> Wrapped { get; init; } = null!; // guaranteed to be set in init code

    public int Count => Wrapped.Count;

    public IServiceProvider ServiceProvider => Wrapped.ServiceProvider;

    public ConquerorContext ConquerorContext => Wrapped.ConquerorContext;

    public MessageTransportType TransportType => Wrapped.TransportType;
    IEnumerator<IMessageMiddleware<TMessage, TResponse>> IEnumerable<IMessageMiddleware<TMessage, TResponse>>.GetEnumerator()
        => Wrapped.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Wrapped).GetEnumerator();

    public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => Wrapped.Use(middleware);

    public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        => Wrapped.Use(middlewareFn);

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => Wrapped.Without<TMiddleware>();

    public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => Wrapped.Configure(configure);
}
