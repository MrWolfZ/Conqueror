using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessagePipeline<TMessage, TResponse>(
    Type? handlerType,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    MessageTransportType transportType,
    IMessageMiddleware<TMessage, TResponse>[] middlewares)
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public MessageTransportType TransportType { get; } = transportType;

    public int Count { get; private set; }

    public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        middlewares[Count] = middleware;
        Count += 1;
        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
    {
        return Use(new DelegateMessageMiddleware(middlewareFn));
    }

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        // _ = middlewares.RemoveAll(static m => m is TMiddleware);

        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        var found = false;
        for (var i = 0; i < Count; i++)
        {
            if (middlewares[i] is TMiddleware m)
            {
                configure(m);
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException($"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used");
        }

        return this;
    }

    public MessagePipelineRunner<TMessage, TResponse> Build(ConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares, Count);
    }

    public IEnumerator<IMessageMiddleware<TMessage, TResponse>> GetEnumerator() => ((IEnumerable<IMessageMiddleware<TMessage, TResponse>>)middlewares).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateMessageMiddleware(MessageMiddlewareFn<TMessage, TResponse> middlewareFn) : IMessageMiddleware<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) => middlewareFn(ctx);
    }
}
