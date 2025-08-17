#pragma warning disable CA1034

namespace Conqueror;

using System.Collections;
using System.ComponentModel;

public delegate Task<TResponse> MessageMiddlewareFn<TMessage, TResponse>(
    MessageMiddlewareContext<TMessage, TResponse> context
)
    where TMessage : class, IMessage<TMessage, TResponse>;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "naming is intentional")]
public interface IMessagePipeline<TMessage, TResponse> : IReadOnlyCollection<IMessageMiddleware<TMessage, TResponse>>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The type of the handler this pipeline is being built for. Is <see langword="null" /> for
    ///     delegate handlers or when the pipeline is being built for a sender.
    /// </summary>
    Type? HandlerType { get; }

    IServiceProvider ServiceProvider { get; }

    IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;

    IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn);

    IMessagePipeline<TMessage, TResponse> UseWhen(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        Action<IMessagePipeline<TMessage, TResponse>> configureConditionalPipeline
    );

    IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse>;

    IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configureFn)
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

    IEnumerator<IMessageMiddleware<TMessage, TResponse>> IEnumerable<
        IMessageMiddleware<TMessage, TResponse>
    >.GetEnumerator() => Wrapped.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Wrapped).GetEnumerator();

    public IMessagePipeline<TMessage, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => Wrapped.Use(middleware);

    public IMessagePipeline<TMessage, TResponse> Use(MessageMiddlewareFn<TMessage, TResponse> middlewareFn) =>
        Wrapped.Use(middlewareFn);

    public IMessagePipeline<TMessage, TResponse> UseWhen(
        Predicate<MessageMiddlewareContext<TMessage, TResponse>> predicate,
        Action<IMessagePipeline<TMessage, TResponse>> configureConditionalPipeline
    ) => Wrapped.UseWhen(predicate, configureConditionalPipeline);

    public IMessagePipeline<TMessage, TResponse> Without<TMiddleware>()
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => Wrapped.Without<TMiddleware>();

    public IMessagePipeline<TMessage, TResponse> Configure<TMiddleware>(Action<TMiddleware> configureFn)
        where TMiddleware : IMessageMiddleware<TMessage, TResponse> => Wrapped.Configure(configureFn);
}
