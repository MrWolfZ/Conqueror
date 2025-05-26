using System;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Base interface for transports to be able to get an injector that works
///     with their specific constraint interface.
/// </summary>
public interface IMessageHandlerTypesInjector
{
    Type MessageType { get; }
}

internal interface ICoreMessageHandlerTypesInjector : IMessageHandlerTypesInjector
{
    Delegate? ConfigurePipeline { get; }

    void ConfigureInProcessReceiver(IInProcessMessageReceiver receiver);

    /// <summary>
    ///     Helper method to be able to access the message types as generic parameters while only
    ///     having a generic reference to the message or handler type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <param name="arg">The argument to pass to the injectable</param>
    /// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Inject<TArg, TResult>(ICoreMessageHandlerTypesInjectable<TArg, TResult> injectable, TArg arg);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CoreMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>(
    Delegate? configurePipeline,
    Action<IInProcessMessageReceiver>? configureInProcessReceiver)
    : ICoreMessageHandlerTypesInjector
    where TMessage : class, IMessage<TMessage, TResponse>
    where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>
    where TProxy : MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy>, TIHandler, new()
    where TIPipeline : class, IMessagePipeline<TMessage, TResponse>
    where TPipelineProxy : MessagePipelineProxy<TMessage, TResponse>, TIPipeline, new()
{
    public Type MessageType { get; } = typeof(TMessage);

    public Delegate? ConfigurePipeline { get; } = configurePipeline;

    public void ConfigureInProcessReceiver(IInProcessMessageReceiver receiver)
    {
        // will be null for delegate handlers
        configureInProcessReceiver?.Invoke(receiver);
    }

    public TResult Inject<TArg, TResult>(ICoreMessageHandlerTypesInjectable<TArg, TResult> injectable, TArg arg)
        => injectable.WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>(arg);
}

/// <summary>
///     Helper interface to be able to access the message types as generic parameters while only
///     having a generic reference to a message handler type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface ICoreMessageHandlerTypesInjectable<in TArg, out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>(TArg arg)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>
        where TProxy : MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy>, TIHandler, new()
        where TIPipeline : class, IMessagePipeline<TMessage, TResponse>
        where TPipelineProxy : MessagePipelineProxy<TMessage, TResponse>, TIPipeline, new();
}
