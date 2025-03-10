using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

#pragma warning disable CA1034

namespace Conqueror;

public delegate Task<TResponse> MessageMiddlewareFn<TMessage, TResponse>(MessageMiddlewareContext<TMessage, TResponse> context)
    where TMessage : class, IMessage<TResponse>;

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

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessagePipeline<TMessage, TResponse, out TGenerated>
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
    where TGenerated : IMessagePipeline<TMessage, TResponse>
{
    static abstract TGenerated Create(IMessagePipeline<TMessage, TResponse> wrapped);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class GeneratedMessagePipelineAdapter<TMessage, TResponse>(IMessagePipeline<TMessage, TResponse> wrapped)
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    public int Count => wrapped.Count;

    public IServiceProvider ServiceProvider => wrapped.ServiceProvider;

    public ConquerorContext ConquerorContext => wrapped.ConquerorContext;

    public MessageTransportType TransportType => wrapped.TransportType;
    IEnumerator<IMessageMiddleware<TMessage, TResponse>> IEnumerable<IMessageMiddleware<TMessage, TResponse>>.GetEnumerator()
        => wrapped.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)wrapped).GetEnumerator();

    public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => wrapped.Use(middleware);

    public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        => wrapped.Use(middlewareFn);

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => wrapped.Without<TMiddleware>();

    public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => wrapped.Configure(configure);
}
