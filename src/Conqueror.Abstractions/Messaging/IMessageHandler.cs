using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageHandler<in TMessage, TResponse> : IMessageHandlerWithTypeInjector
    where TMessage : class, IMessage<TMessage, TResponse>
{
    static IDefaultMessageTypesInjector IMessageHandlerWithTypeInjector.DefaultTypeInjector
        => TMessage.DefaultTypeInjector;

    Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default);
}

public interface IMessageHandler<in TMessage> : IMessageHandlerWithTypeInjector
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
{
    static IDefaultMessageTypesInjector IMessageHandlerWithTypeInjector.DefaultTypeInjector
        => TMessage.DefaultTypeInjector;

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
public interface IMessageHandlerWithTypeInjector
{
    internal static abstract IDefaultMessageTypesInjector DefaultTypeInjector { get; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler : IMessageHandlerWithTypeInjector;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, TResponse, in TPipelineInterface>
    : IMessageHandler<TMessage, TResponse>, IGeneratedMessageHandler
    where TMessage : class, IMessage<TMessage, TResponse>
    where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
{
    static virtual void ConfigurePipeline(TPipelineInterface pipeline)
    {
        // by default, we use an empty pipeline
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, in TPipelineInterface>
    : IMessageHandler<TMessage>, IGeneratedMessageHandler
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
{
    static virtual void ConfigurePipeline(TPipelineInterface pipeline)
    {
        // by default, we use an empty pipeline
    }
}
