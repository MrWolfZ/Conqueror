using System;

namespace Conqueror.Messaging;

internal sealed class MessageSenders(
    IServiceProvider serviceProvider,
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory)
    : IMessageSenders
{
    public TIHandler For<TMessage, TResponse, TIHandler>(MessageTypes<TMessage, TResponse, TIHandler> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return TMessage.CoreTypesInjector.Create(
            new Injectable<TIHandler>(
                serviceProvider,
                conquerorContextAccessor,
                messageIdFactory));
    }

    private readonly struct Injectable<TIHandlerParam>(
        IServiceProvider serviceProvider,
        IConquerorContextAccessor conquerorContextAccessor,
        IMessageIdFactory messageIdFactory) : ICoreMessageHandlerTypesInjectable<TIHandlerParam>
        where TIHandlerParam : class
    {
        TIHandlerParam ICoreMessageHandlerTypesInjectable<TIHandlerParam>
            .WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler>()
        {
            var dispatcher = new MessageDispatcher<TMessage, TResponse>(
                serviceProvider,
                conquerorContextAccessor,
                messageIdFactory,
                MessageSenderFactory<TMessage, TResponse>.InProcess,
                null,
                MessageTransportRole.Sender,
                null);

            var proxy = new TProxy
            {
                Dispatcher = dispatcher,
            };

            return proxy as TIHandlerParam ?? throw new InvalidOperationException("could not create handler proxy");
        }
    }
}
