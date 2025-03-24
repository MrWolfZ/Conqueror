using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageClients(IServiceProvider serviceProvider) : IMessageClients
{
    public IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>
        => CreateProxy<TMessage, TResponse>(serviceProvider);

    public IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(MessageTypes<TMessage, TResponse> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>
        => CreateProxy<TMessage, TResponse>(serviceProvider);

    public IMessageHandler<TMessage> For<TMessage>()
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        => new MessageWithoutResponseHandlerAdapter<TMessage> { Wrapped = CreateProxy<TMessage, UnitMessageResponse>(serviceProvider) };

    public IMessageHandler<TMessage> For<TMessage>(MessageTypes<TMessage, UnitMessageResponse> messageTypes)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        => new MessageWithoutResponseHandlerAdapter<TMessage> { Wrapped = CreateProxy<TMessage, UnitMessageResponse>(serviceProvider) };

    private static MessageHandlerProxy<TMessage, TResponse> CreateProxy<TMessage, TResponse>(
        IServiceProvider serviceProvider)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return new(
            serviceProvider,
            new(b => b.UseInProcess()),
            null,
            MessageTransportRole.Client);
    }

    private sealed class MessageWithoutResponseHandlerAdapter<TMessage>
        : IConfigurableMessageHandler<TMessage>
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        public IConfigurableMessageHandler<TMessage, UnitMessageResponse> Wrapped { get; init; } = null!; // guaranteed to be set in init code

        public Task Handle(TMessage message, CancellationToken cancellationToken = default)
            => Wrapped.Handle(message, cancellationToken);

        public IMessageHandler<TMessage> WithPipeline(Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
            => new MessageWithoutResponseHandlerAdapter<TMessage>
            {
                Wrapped = (IConfigurableMessageHandler<TMessage, UnitMessageResponse>)Wrapped.WithPipeline(configurePipeline),
            };

        public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport)
            => new MessageWithoutResponseHandlerAdapter<TMessage>
            {
                Wrapped = (IConfigurableMessageHandler<TMessage, UnitMessageResponse>)Wrapped.WithTransport(configureTransport),
            };

        public IMessageHandler<TMessage> WithTransport(ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport)
            => new MessageWithoutResponseHandlerAdapter<TMessage>
            {
                Wrapped = (IConfigurableMessageHandler<TMessage, UnitMessageResponse>)Wrapped.WithTransport(configureTransport),
            };
    }
}
