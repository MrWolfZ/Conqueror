using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Conqueror.MessagePipelineConditionalExtensions;

namespace Conqueror.Messaging;

internal sealed class MessagePipeline<TMessage, TResponse>(
    Type? handlerType,
    IServiceProvider serviceProvider)
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly List<IMessageMiddleware<TMessage, TResponse>> middlewares = [];

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public int Count => middlewares.Count;

    internal int Capacity => middlewares.Capacity;

    public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        middlewares.Add(middleware);

        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
    {
        return Use(new DelegateMessageMiddleware(middlewareFn));
    }

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        _ = middlewares.RemoveAll(static m => m is TMiddleware or ConditionalMessageMiddleware<TMessage, TResponse, TMiddleware>);

        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        var found = false;
        foreach (var middleware in middlewares)
        {
            if (middleware is TMiddleware m)
            {
                configure(m);
                found = true;
            }

            if (middleware is ConditionalMessageMiddleware<TMessage, TResponse, TMiddleware> conditionalMiddleware)
            {
                configure(conditionalMiddleware.Middleware);
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException($"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used");
        }

        return this;
    }

    public Task<TResponse> Execute(
        IServiceProvider serviceProvider,
        TMessage message,
        IMessageSender<TMessage, TResponse> sender,
        MessageTransportType transportType,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return sender.Send(
                message,
                serviceProvider,
                conquerorContext,
                cancellationToken);
        }

        var ctx = new MessageMiddlewareContext<TMessage, TResponse>(middlewares, sender)
        {
            Message = message,
            TransportType = transportType,
            CancellationToken = cancellationToken,
            ConquerorContext = conquerorContext,
            ServiceProvider = serviceProvider,
        };

        return middlewares[0].Execute(ctx);
    }

    public IEnumerator<IMessageMiddleware<TMessage, TResponse>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateMessageMiddleware(MessageMiddlewareFn<TMessage, TResponse> middlewareFn) : IMessageMiddleware<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) => middlewareFn(ctx);
    }
}
