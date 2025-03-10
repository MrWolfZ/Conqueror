using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

internal sealed class MessageClients(IServiceProvider serviceProvider) : IMessageClients
{
    public THandler For<THandler>()
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        return THandler.Create<THandler>(CreateProxyFactory()) ?? throw new InvalidOperationException($"cannot create handler for type '{typeof(THandler)}'");
    }

    public IMessageHandler<TMessage, TResponse> ForMessageType<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>
    {
        return CreateProxyFactory().CreateProxy<TMessage, TResponse>();
    }

    public IMessageHandler<TMessage> ForMessageType<TMessage>()
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return new MessageHandlerWithoutResponseAdapter<TMessage>(CreateProxyFactory().CreateProxy<TMessage, UnitMessageResponse>());
    }

    private MessageHandlerProxyFactory CreateProxyFactory()
    {
        return new(serviceProvider, MessageTransportRole.Client);
    }

    private sealed class MessageHandlerWithoutResponseAdapter<TMessage>(IConfigurableMessageHandler<TMessage, UnitMessageResponse> wrapped)
        : IConfigurableMessageHandler<TMessage>
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        public Task Handle(TMessage message, CancellationToken cancellationToken = default)
            => wrapped.Handle(message, cancellationToken);

        public IMessageHandler<TMessage> WithPipeline(Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
            => new MessageHandlerWithoutResponseAdapter<TMessage>((IConfigurableMessageHandler<TMessage, UnitMessageResponse>)wrapped.WithPipeline(configurePipeline));

        public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport)
            => new MessageHandlerWithoutResponseAdapter<TMessage>((IConfigurableMessageHandler<TMessage, UnitMessageResponse>)wrapped.WithTransport(configureTransport));

        public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport)
            => new MessageHandlerWithoutResponseAdapter<TMessage>((IConfigurableMessageHandler<TMessage, UnitMessageResponse>)wrapped.WithTransport(configureTransport));
    }
}
