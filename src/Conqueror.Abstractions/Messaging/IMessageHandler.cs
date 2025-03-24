using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageHandler<in TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default);
}

public interface IMessageHandler<in TMessage>
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
{
    Task Handle(TMessage message, CancellationToken cancellationToken = default);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConfigurableMessageHandler<TMessage, TResponse> : IMessageHandler<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    IMessageHandler<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline);

    IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClient<TMessage, TResponse> configureTransport);

    IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConfigurableMessageHandler<TMessage> : IMessageHandler<TMessage>
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
{
    IMessageHandler<TMessage> WithPipeline(Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline);

    IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport);

    IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler
{
    /// <summary>
    ///     Helper method to be able to access the types related to a message type as generic parameters while only
    ///     having a generic reference to the generated handler interface type. This obviates the need for reflection.
    /// </summary>
    /// <param name="factory">The factory that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static abstract TResult CreateWithMessageTypes<TResult>(IMessageTypesInjectionFactory<TResult> factory);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, TResponse, THandlerInterface, in TPipelineInterface, TPipelineAdapter>
    : IMessageHandler<TMessage, TResponse>, IGeneratedMessageHandler
    where TMessage : class, IMessage<TMessage, TResponse>
    where THandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, THandlerInterface, TPipelineInterface, TPipelineAdapter>
    where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
    where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
{
    static virtual void ConfigurePipeline(TPipelineInterface pipeline)
    {
        // by default, we use an empty pipeline
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static TResult IGeneratedMessageHandler.CreateWithMessageTypes<TResult>(IMessageTypesInjectionFactory<TResult> factory)
        => factory.Create<TMessage, TResponse, THandlerInterface, TPipelineInterface, TPipelineAdapter>();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, THandlerInterface, in TPipelineInterface, TPipelineAdapter>
    : IMessageHandler<TMessage>, IGeneratedMessageHandler
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    where THandlerInterface : class, IGeneratedMessageHandler<TMessage, THandlerInterface, TPipelineInterface, TPipelineAdapter>
    where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
    where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
{
    static virtual void ConfigurePipeline(TPipelineInterface pipeline)
    {
        // by default, we use an empty pipeline
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static TResult IGeneratedMessageHandler.CreateWithMessageTypes<TResult>(IMessageTypesInjectionFactory<TResult> factory)
        => factory.Create<TMessage, THandlerInterface, TPipelineInterface, TPipelineAdapter>();
}
