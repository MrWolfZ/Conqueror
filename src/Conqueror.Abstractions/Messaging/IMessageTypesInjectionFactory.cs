using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Helper interface to be able to access the types related to a message type as generic parameters while only
///     having a generic reference to the generated handler interface type. This obviates the need for reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageTypesInjectionFactory<out TResult>
{
    TResult Create<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>()
        where TMessage : class, IMessage<TMessage, TResponse>
        where THandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
        where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage, TResponse>, THandlerInterface, new()
        where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
        where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new();

    TResult Create<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>()
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where THandlerInterface : class, IGeneratedMessageHandler<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
        where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage>, THandlerInterface, new()
        where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
        where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new();
}
