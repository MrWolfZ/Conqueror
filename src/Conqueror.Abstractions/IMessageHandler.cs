using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
#pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageHandler
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract Type MessageType();

    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract Type ResponseType();

    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract Delegate? CreateConfigurePipeline<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] THandler>()
        where THandler : class, IMessageHandler;
}

public interface IMessageHandler<in TMessage, TResponse> : IMessageHandler
    where TMessage : class, IMessage<TResponse>
{
    Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default);

    [EditorBrowsable(EditorBrowsableState.Never)]
    static Type IMessageHandler.MessageType() => typeof(TMessage);

    [EditorBrowsable(EditorBrowsableState.Never)]
    static Type IMessageHandler.ResponseType() => typeof(TResponse);

    [EditorBrowsable(EditorBrowsableState.Never)]
    static Delegate IMessageHandler.CreateConfigurePipeline<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] THandler>()
        => throw new NotSupportedException($"method '{nameof(IMessageHandler.CreateConfigurePipeline)}' should never be called on this type");
}

public interface IMessageHandler<in TMessage> : IMessageHandler<TMessage, UnitMessageResponse>
    where TMessage : class, IMessage<UnitMessageResponse>
{
    new Task Handle(TMessage message, CancellationToken cancellationToken = default);

    [EditorBrowsable(EditorBrowsableState.Never)]
    async Task<UnitMessageResponse> IMessageHandler<TMessage, UnitMessageResponse>.Handle(TMessage message, CancellationToken cancellationToken)
    {
        await Handle(message, cancellationToken).ConfigureAwait(false);
        return UnitMessageResponse.Instance;
    }
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
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract THandlerInterface? Create<THandlerInterface>(IMessageHandlerProxyFactory proxyFactory)
        where THandlerInterface : class, IGeneratedMessageHandler;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, TResponse, in TPipeline> : IMessageHandler<TMessage, TResponse>, IGeneratedMessageHandler
    where TMessage : class, IMessage<TResponse>
    where TPipeline : IGeneratedMessagePipeline<TMessage, TResponse, TPipeline>
{
    static virtual void ConfigurePipeline(TPipeline pipeline)
    {
        // by default, we use an empty pipeline
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static Delegate? IMessageHandler.CreateConfigurePipeline<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IGeneratedMessageHandler<TMessage, TResponse, TPipeline>)),
                     $"handler type should implement {nameof(IGeneratedMessageHandler<TMessage, TResponse, TPipeline>)}");

        var methodInfo = typeof(THandler).GetMethod(nameof(ConfigurePipeline), BindingFlags.Public | BindingFlags.Static);

        if (methodInfo == null)
        {
            return null;
        }

        var configure = (Action<TPipeline>)Delegate.CreateDelegate(typeof(Action<TPipeline>), methodInfo);
        return (IMessagePipeline<TMessage, TResponse> pipeline) => configure(TPipeline.Create(pipeline));
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandler<in TMessage, in TPipeline> : IMessageHandler<TMessage>, IGeneratedMessageHandler
    where TMessage : class, IMessage<UnitMessageResponse>
    where TPipeline : IGeneratedMessagePipeline<TMessage, UnitMessageResponse, TPipeline>
{
    static virtual void ConfigurePipeline(TPipeline pipeline)
    {
        // by default, we use an empty pipeline
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    static Delegate? IMessageHandler.CreateConfigurePipeline<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IGeneratedMessageHandler<TMessage, TPipeline>)),
                     $"handler type should implement {nameof(IGeneratedMessageHandler<TMessage, TPipeline>)}");

        var methodInfo = typeof(THandler).GetMethod(nameof(ConfigurePipeline), BindingFlags.Public | BindingFlags.Static);

        if (methodInfo == null)
        {
            return null;
        }

        var configure = (Action<TPipeline>)Delegate.CreateDelegate(typeof(Action<TPipeline>), methodInfo);
        return (IMessagePipeline<TMessage, UnitMessageResponse> pipeline) => configure(TPipeline.Create(pipeline));
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedMessageHandlerAdapter;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class GeneratedMessageHandlerAdapter<TMessage, TResponse>(IConfigurableMessageHandler<TMessage, TResponse> wrapped)
    : IConfigurableMessageHandler<TMessage, TResponse>, IGeneratedMessageHandlerAdapter
    where TMessage : class, IMessage<TResponse>
{
    public Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
        => wrapped.Handle(message, cancellationToken);

    public IMessageHandler<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        => wrapped.WithPipeline(configurePipeline);

    public IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClient<TMessage, TResponse> configureTransport)
        => wrapped.WithTransport(configureTransport);

    public IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport)
        => wrapped.WithTransport(configureTransport);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class GeneratedMessageHandlerAdapter<TMessage>(IConfigurableMessageHandler<TMessage, UnitMessageResponse> wrapped)
    : IConfigurableMessageHandler<TMessage>, IGeneratedMessageHandlerAdapter
    where TMessage : class, IMessage<UnitMessageResponse>
{
    public Task Handle(TMessage message, CancellationToken cancellationToken = default)
        => wrapped.Handle(message, cancellationToken);

    public IMessageHandler<TMessage> WithPipeline(Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        => new GeneratedMessageHandlerAdapter<TMessage>((IConfigurableMessageHandler<TMessage, UnitMessageResponse>)wrapped.WithPipeline(configurePipeline));

    public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport)
        => new GeneratedMessageHandlerAdapter<TMessage>((IConfigurableMessageHandler<TMessage, UnitMessageResponse>)wrapped.WithTransport(configureTransport));

    public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport)
        => new GeneratedMessageHandlerAdapter<TMessage>((IConfigurableMessageHandler<TMessage, UnitMessageResponse>)wrapped.WithTransport(configureTransport));
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageHandlerProxyFactory
{
    IConfigurableMessageHandler<TMessage, TResponse> CreateProxy<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>;
}
