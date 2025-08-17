namespace Conqueror.Messaging;

using System.Collections;

internal sealed class MessagePipeline<TMessage, TResponse>(Type? handlerType, IServiceProvider serviceProvider)
    : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly List<IMessageMiddleware<TMessage, TResponse>> middlewares = [];

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public int Count => middlewares.Count;

    public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        middlewares.Add(middleware);

        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn) =>
        Use(new DelegateMessageMiddleware(middlewareFn));

    public IMessagePipeline<TMessage, TResponse> UseWhen(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        Action<IMessagePipeline<TMessage, TResponse>> configureConditionalPipeline
    )
    {
        configureConditionalPipeline(new ConditionalPipeline(predicate, this));

        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        _ = middlewares.RemoveAll(static m => m is TMiddleware or ConditionalMessageMiddleware<TMiddleware>);

        return this;
    }

    public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configureFn)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        var found = false;
        foreach (var middleware in middlewares)
        {
            if (middleware is TMiddleware m)
            {
                configureFn(m);
                found = true;
            }

            if (middleware is ConditionalMessageMiddleware<TMiddleware> conditionalMiddleware)
            {
                configureFn(conditionalMiddleware.Middleware);
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException(
                $"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used"
            );
        }

        return this;
    }

    public IEnumerator<IMessageMiddleware<TMessage, TResponse>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Task<TResponse> Execute(
        IServiceProvider serviceProvider,
        TMessage message,
        IMessageSender<TMessage, TResponse> sender,
        MessageTransportType transportType,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
    {
        if (middlewares.Count is 0)
        {
            return sender.Send(message, serviceProvider, conquerorContext, cancellationToken);
        }

        var ctx = new MessageMiddlewareContext<TMessage, TResponse>(
            middlewares,
            sender,
            serviceProvider,
            conquerorContext,
            transportType
        )
        {
            Message = message,
            CancellationToken = cancellationToken,
        };

        return middlewares[0].Execute(ctx);
    }

    private sealed class DelegateMessageMiddleware(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        : IMessageMiddleware<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) => middlewareFn(ctx);
    }

    private sealed class ConditionalMessageMiddleware<TMiddleware>(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        TMiddleware middleware
    ) : IMessageMiddleware<TMessage, TResponse>
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        public TMiddleware Middleware => middleware;

        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) =>
            predicate(ctx) ? middleware.Execute(ctx) : ctx.Next(ctx.Message, ctx.CancellationToken);
    }

    private sealed class ConditionalDelegateMessageMiddleware(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        MessageMiddlewareFn<TMessage, TResponse> middlewareFn
    ) : IMessageMiddleware<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) =>
            predicate(ctx) ? middlewareFn(ctx) : ctx.Next(ctx.Message, ctx.CancellationToken);
    }

    private sealed class ConditionalPipeline(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> outerPredicate,
        IMessagePipeline<TMessage, TResponse> pipeline
    ) : IMessagePipeline<TMessage, TResponse>
    {
        public Type? HandlerType => pipeline.HandlerType;

        public IServiceProvider ServiceProvider => pipeline.ServiceProvider;

        public int Count => pipeline.Count;

        public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : IMessageMiddleware<TMessage, TResponse>
        {
            _ = pipeline.Use(new ConditionalMessageMiddleware<TMiddleware>(outerPredicate, middleware));

            return this;
        }

        public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        {
            _ = pipeline.Use(new ConditionalDelegateMessageMiddleware(outerPredicate, middlewareFn));

            return this;
        }

        public IMessagePipeline<TMessage, TResponse> UseWhen(
            Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
            Action<IMessagePipeline<TMessage, TResponse>> configureConditionalPipeline
        ) => pipeline.UseWhen(ctx => outerPredicate(ctx) && predicate(ctx), configureConditionalPipeline);

        public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
            where TMiddleware : IMessageMiddleware<TMessage, TResponse> => pipeline.Without<TMiddleware>();

        public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configureFn)
            where TMiddleware : IMessageMiddleware<TMessage, TResponse> => pipeline.Configure(configureFn);

        IEnumerator<IMessageMiddleware<TMessage, TResponse>> IEnumerable<
            IMessageMiddleware<TMessage, TResponse>
        >.GetEnumerator() => pipeline.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)pipeline).GetEnumerator();
    }
}
