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
    TResult Create<TResult>(ICoreMessageHandlerTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CoreMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler> : ICoreMessageHandlerTypesInjector
    where TMessage : class, IMessage<TMessage, TResponse>
    where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>
    where TProxy : MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy>, TIHandler, new()
    where TIPipeline : class, IMessagePipeline<TMessage, TResponse>
    where TPipelineProxy : MessagePipelineProxy<TMessage, TResponse>, TIPipeline, new()
    where THandler : class, TIHandler
{
    public static readonly CoreMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler> Default = new();

    public Type MessageType { get; } = typeof(TMessage);

    /// <summary>
    ///     Helper method to be able to access the message types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult Create<TResult>(ICoreMessageHandlerTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler>();
}

/// <summary>
///     Helper interface to be able to access the message types as generic parameters while only
///     having a generic reference to a message handler type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface ICoreMessageHandlerTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler>()
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>
        where TProxy : MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy>, TIHandler, new()
        where TIPipeline : class, IMessagePipeline<TMessage, TResponse>
        where TPipelineProxy : MessagePipelineProxy<TMessage, TResponse>, TIPipeline, new()
        where THandler : class, TIHandler;
}
