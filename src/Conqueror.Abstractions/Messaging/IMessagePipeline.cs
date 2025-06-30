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
    /// <summary>
    ///     The type of the handler this pipeline is being built for. Is <c>null</c> for
    ///     delegate handlers or when the pipeline is being built for a sender.
    /// </summary>
    Type? HandlerType { get; }

    IServiceProvider ServiceProvider { get; }

    IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;

    IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn);

    IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;

    IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class MessagePipelineProxy<TMessage, TResponse> : IMessagePipeline<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public int Count => Wrapped.Count;

    public Type? HandlerType => Wrapped.HandlerType;

    public IServiceProvider ServiceProvider => Wrapped.ServiceProvider;

    internal IMessagePipeline<TMessage, TResponse> Wrapped { get; init; } = null!; // guaranteed to be set in init code

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

public static class MessagePipelineConditionalExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseWhen<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return new ConditionalPipeline<TMessage, TResponse>(predicate, pipeline);
    }

    internal sealed class ConditionalMessageMiddleware<TMessage, TResponse, TMiddleware>(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        TMiddleware middleware)
        : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>
    {
        public TMiddleware Middleware => middleware;

        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
            => predicate(ctx) ? middleware.Execute(ctx) : ctx.Next(ctx.Message, ctx.CancellationToken);
    }

    private sealed class ConditionalDelegateMessageMiddleware<TMessage, TResponse>(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
            => predicate(ctx) ? middlewareFn(ctx) : ctx.Next(ctx.Message, ctx.CancellationToken);
    }

    private sealed class ConditionalPipeline<TMessage, TResponse>(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        IMessagePipeline<TMessage, TResponse> pipeline)
        : IMessagePipeline<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public Type? HandlerType => pipeline.HandlerType;

        public IServiceProvider ServiceProvider => pipeline.ServiceProvider;

        public int Count => pipeline.Count;

        public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : IMessageMiddleware<TMessage, TResponse>
        {
            _ = pipeline.Use(new ConditionalMessageMiddleware<TMessage, TResponse, TMiddleware>(predicate, middleware));

            return this;
        }

        public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn)
        {
            _ = pipeline.Use(new ConditionalDelegateMessageMiddleware<TMessage, TResponse>(predicate, middlewareFn));

            return this;
        }

        IMessagePipeline<TMessage, TResponse> IMessagePipeline<TMessage, TResponse>.Without<TMiddleware>()
            => pipeline.Without<TMiddleware>();

        IMessagePipeline<TMessage, TResponse> IMessagePipeline<TMessage, TResponse>.Configure<TMiddleware>(Action<TMiddleware> configure)
            => pipeline.Configure(configure);

        IEnumerator<IMessageMiddleware<TMessage, TResponse>> IEnumerable<IMessageMiddleware<TMessage, TResponse>>.GetEnumerator()
            => pipeline.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)pipeline).GetEnumerator();
    }
}
