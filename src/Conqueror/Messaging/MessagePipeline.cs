using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessagePipeline<TMessage, TResponse>(
    Type? handlerType,
    IServiceProvider serviceProvider,
    MessageTransportType transportType,
    int initialCapacity)
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly List<IMessageMiddleware<TMessage, TResponse>> middlewares = new(initialCapacity);

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public MessageTransportType TransportType { get; } = transportType;

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

    public IConditionalMessagePipeline<TMessage, TResponse> UseWhen(Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate)
    {
        return new ConditionalPipeline(predicate, this);
    }

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        _ = middlewares.RemoveAll(static m => m is TMiddleware or ConditionalMessageMiddleware<TMiddleware>);

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

            if (middleware is ConditionalMessageMiddleware<TMiddleware> conditionalMiddleware)
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
        TMessage message,
        IMessageSender<TMessage, TResponse> sender,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return sender.Send(
                message,
                ServiceProvider,
                conquerorContext,
                cancellationToken);
        }

        var ctx = new MessageMiddlewareContext<TMessage, TResponse>(middlewares, sender)
        {
            Message = message,
            TransportType = TransportType,
            CancellationToken = cancellationToken,
            ConquerorContext = conquerorContext,
            ServiceProvider = ServiceProvider,
        };

        return middlewares[0].Execute(ctx);
    }

    public IEnumerator<IMessageMiddleware<TMessage, TResponse>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateMessageMiddleware(MessageMiddlewareFn<TMessage, TResponse> middlewareFn) : IMessageMiddleware<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) => middlewareFn(ctx);
    }

    private sealed class ConditionalMessageMiddleware<TMiddleware>(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        TMiddleware middleware)
        : IMessageMiddleware<TMessage, TResponse>
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        public TMiddleware Middleware => middleware;

        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
            => predicate(ctx) ? middleware.Execute(ctx) : ctx.Next(ctx.Message, ctx.CancellationToken);
    }

    private sealed class ConditionalDelegateMessageMiddleware(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        : IMessageMiddleware<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
            => predicate(ctx) ? middlewareFn(ctx) : ctx.Next(ctx.Message, ctx.CancellationToken);
    }

    private sealed class ConditionalPipeline(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        MessagePipeline<TMessage, TResponse> pipeline)
        : IConditionalMessagePipeline<TMessage, TResponse>
    {
        public IConditionalMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : IMessageMiddleware<TMessage, TResponse>
        {
            _ = pipeline.Use(new ConditionalMessageMiddleware<TMiddleware>(predicate, middleware));

            return this;
        }

        public IConditionalMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        {
            _ = pipeline.Use(new ConditionalDelegateMessageMiddleware(predicate, middlewareFn));

            return this;
        }
    }
}
