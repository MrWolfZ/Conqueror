using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageHandler
{
    /// <summary>
    ///     Implemented by source generator for each handler type. Cannot be abstract since otherwise
    ///     the generated <code>IHandler</code> types could not be used as generic arguments.
    /// </summary>
    static virtual IEnumerable<IMessageHandlerTypesInjector> GetTypeInjectors()
        => throw new NotSupportedException("this should be implemented by the source generator for each concrete handler type");

    static virtual void ConfigureInProcessReceiver(IInProcessMessageReceiver receiver)
    {
        // we don't configure the receiver (by default, it is enabled for all message types)
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IMessageHandler<TMessage, TResponse, TIHandler> : IMessageHandler
    where TMessage : class, IMessage<TMessage, TResponse>
    where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
{
    static virtual MessageTypes<TMessage, TResponse, TIHandler> MessageTypes { get; } = new();

    static virtual Task<TResponse> Invoke(TIHandler handler, TMessage message, CancellationToken cancellationToken)
        => throw new NotSupportedException("this should be implemented by the source generator for each concrete handler interface type");
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageHandler<TMessage, TResponse, TIHandler, TProxy, in TIPipeline, TPipelineProxy>
    : IMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, IMessage<TMessage, TResponse>
    where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>
    where TProxy : MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy>, TIHandler, new()
    where TIPipeline : class, IMessagePipeline<TMessage, TResponse>
    where TPipelineProxy : MessagePipelineProxy<TMessage, TResponse>, TIPipeline, new()
{
    static virtual void ConfigurePipeline(TIPipeline pipeline)
    {
        // by default, we use an empty pipeline
    }

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static IMessageHandlerTypesInjector CreateCoreTypesInjector<THandler>()
        where THandler : class, TIHandler
        => CoreMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler>.Default;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy> : IMessageHandlerProxy<TMessage, TResponse, TIHandler>
    where TMessage : class, IMessage<TMessage, TResponse>
    where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    where TProxy : MessageHandlerProxy<TMessage, TResponse, TIHandler, TProxy>, TIHandler, new()
{
    // cannot be 'required' since that would block the `new()` constraint
    internal IMessageDispatcher<TMessage, TResponse> Dispatcher { get; init; } = null!;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
        => Dispatcher.Dispatch(message, cancellationToken);

    public TIHandler WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        => new TProxy { Dispatcher = Dispatcher.WithPipeline(configurePipeline) };

    public TIHandler WithTransport(ConfigureMessageSender<TMessage, TResponse> configureTransport)
        => new TProxy { Dispatcher = Dispatcher.WithSender(configureTransport) };

    public TIHandler WithTransport(ConfigureMessageSenderAsync<TMessage, TResponse> configureTransport)
        => new TProxy { Dispatcher = Dispatcher.WithSender(configureTransport) };

    static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors()
        => throw new NotSupportedException("this method should never be called on the proxy");
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class MessageHandlerProxy<TMessage, TIHandler, TProxy> : MessageHandlerProxy<TMessage, UnitMessageResponse, TIHandler, TProxy>
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    where TIHandler : class, IMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    where TProxy : MessageHandlerProxy<TMessage, UnitMessageResponse, TIHandler, TProxy>, TIHandler, new()
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new Task Handle(TMessage message, CancellationToken cancellationToken = default)
        => Dispatcher.Dispatch(message, cancellationToken);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IMessageHandlerProxy<TMessage, TResponse, THandler> : IMessageHandler<TMessage, TResponse, THandler>
    where TMessage : class, IMessage<TMessage, TResponse>
    where THandler : class, IMessageHandler<TMessage, TResponse, THandler>
{
    THandler WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline);

    THandler WithTransport(ConfigureMessageSender<TMessage, TResponse> configureTransport);

    THandler WithTransport(ConfigureMessageSenderAsync<TMessage, TResponse> configureTransport);
}
