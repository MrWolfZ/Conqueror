using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

internal sealed class MessageClients(IServiceProvider serviceProvider) : IMessageClients
{
    public THandler For<THandler>()
        where THandler : class, IGeneratedMessageHandler
    {
        return THandler.CreateWithMessageTypes(new ProxyTypeInjectionFactory<THandler>(serviceProvider))
               ?? throw new InvalidOperationException($"cannot create handler for type '{typeof(THandler)}'");
    }

    public IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(IMessageTypes<TMessage, TResponse> messageTypes)
        where TMessage : class, IMessage<TResponse>
        => CreateProxy<TMessage, TResponse>(serviceProvider);

    public IMessageHandler<TMessage, UnitMessageResponse> For<TMessage>(IMessageTypes<TMessage, UnitMessageResponse> messageTypes)
        where TMessage : class, IMessage<UnitMessageResponse>
        => CreateProxy<TMessage, UnitMessageResponse>(serviceProvider);

    public IMessageHandler<TMessage, TResponse> ForMessageType<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>
        => CreateProxy<TMessage, TResponse>(serviceProvider);

    public IMessageHandler<TMessage> ForMessageType<TMessage>()
        where TMessage : class, IMessage<UnitMessageResponse>
        => new MessageWithoutResponseHandlerAdapter<TMessage> { Wrapped = CreateProxy<TMessage, UnitMessageResponse>(serviceProvider) };

    private static MessageHandlerProxy<TMessage, TResponse> CreateProxy<TMessage, TResponse>(
        IServiceProvider serviceProvider)
        where TMessage : class, IMessage<TResponse>
    {
        return new(
            serviceProvider,
            new(b => b.UseInProcess()),
            null,
            MessageTransportRole.Client);
    }

    private sealed class ProxyTypeInjectionFactory<THandler>(IServiceProvider serviceProvider)
        : IMessageTypesInjectionFactory<THandler?>
        where THandler : class, IGeneratedMessageHandler
    {
        public THandler? Create<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>()
            where TMessage : class, IMessage<TResponse>
            where THandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
            where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage, TResponse>, THandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
            => new THandlerAdapter { Wrapped = CreateProxy<TMessage, TResponse>(serviceProvider) } as THandler;

        public THandler? Create<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>()
            where TMessage : class, IMessage<UnitMessageResponse>
            where THandlerInterface : class, IGeneratedMessageHandler<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
            where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage>, THandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
        {
            var adapter = new THandlerAdapter
            {
                Wrapped = new MessageWithoutResponseHandlerAdapter<TMessage>
                {
                    Wrapped = CreateProxy<TMessage, UnitMessageResponse>(serviceProvider)
                },
            };

            return adapter as THandler;
        }
    }

    private sealed class MessageWithoutResponseHandlerAdapter<TMessage>
        : IConfigurableMessageHandler<TMessage>
        where TMessage : class, IMessage<UnitMessageResponse>
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
