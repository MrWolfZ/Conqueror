using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageHandler<in TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default);
}

public interface IMessageHandler<in TMessage>
    where TMessage : class, IMessage<UnitMessageResponse>
{
    Task Handle(TMessage message, CancellationToken cancellationToken = default);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConfigurableMessageHandler<TMessage, TResponse> : IMessageHandler<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    IMessageHandler<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline);

    IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClient<TMessage, TResponse> configureTransport);

    IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConfigurableMessageHandler<TMessage> : IMessageHandler<TMessage>
    where TMessage : class, IMessage<UnitMessageResponse>
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
public interface IGeneratedMessageHandler<in TMessage, TResponse, THandlerInterface, THandlerAdapter, in TPipelineInterface, TPipelineAdapter>
    : IMessageHandler<TMessage, TResponse>, IGeneratedMessageHandler
    where TMessage : class, IMessage<TResponse>
    where THandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
    where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage, TResponse>, THandlerInterface, new()
    where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
    where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
{
    static virtual void ConfigurePipeline(TPipelineInterface pipeline)
    {
        // by default, we use an empty pipeline
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static TResult IGeneratedMessageHandler.CreateWithMessageTypes<TResult>(IMessageTypesInjectionFactory<TResult> factory)
        => factory.Create<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, THandlerInterface, THandlerAdapter, in TPipelineInterface, TPipelineAdapter>
    : IMessageHandler<TMessage>, IGeneratedMessageHandler
    where TMessage : class, IMessage<UnitMessageResponse>
    where THandlerInterface : class, IGeneratedMessageHandler<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
    where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage>, THandlerInterface, new()
    where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
    where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
{
    static virtual void ConfigurePipeline(TPipelineInterface pipeline)
    {
        // by default, we use an empty pipeline
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static TResult IGeneratedMessageHandler.CreateWithMessageTypes<TResult>(IMessageTypesInjectionFactory<TResult> factory)
        => factory.Create<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandlerAdapter;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class GeneratedMessageHandlerAdapter<TMessage, TResponse>
    : IConfigurableMessageHandler<TMessage, TResponse>, IGeneratedMessageHandlerAdapter
    where TMessage : class, IMessage<TResponse>
{
    public IConfigurableMessageHandler<TMessage, TResponse> Wrapped { get; init; } = null!; // guaranteed to be set in init code

    public Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
        => Wrapped.Handle(message, cancellationToken);

    public IMessageHandler<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        => Wrapped.WithPipeline(configurePipeline);

    public IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClient<TMessage, TResponse> configureTransport)
        => Wrapped.WithTransport(configureTransport);

    public IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport)
        => Wrapped.WithTransport(configureTransport);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class GeneratedMessageHandlerAdapter<TMessage>
    : IConfigurableMessageHandler<TMessage>, IGeneratedMessageHandlerAdapter
    where TMessage : class, IMessage<UnitMessageResponse>
{
    public IConfigurableMessageHandler<TMessage> Wrapped { get; init; } = null!; // guaranteed to be set in init code

    public Task Handle(TMessage message, CancellationToken cancellationToken = default)
        => Wrapped.Handle(message, cancellationToken);

    public IMessageHandler<TMessage> WithPipeline(Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        => Wrapped.WithPipeline(configurePipeline);

    public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport)
        => Wrapped.WithTransport(configureTransport);

    public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport)
        => Wrapped.WithTransport(configureTransport);
}
