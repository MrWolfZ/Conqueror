using System;

namespace Conqueror.Messaging;

internal sealed class MessageSenders(
    IServiceProvider serviceProvider,
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory)
    : IMessageSenders
{
    private static readonly Injectable HandlerCreationInjectable = new();

    public TIHandler For<TMessage, TResponse, TIHandler>(MessageTypes<TMessage, TResponse, TIHandler> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return ((ICoreMessageHandlerTypesInjector)TMessage.CoreTypesInjector).Inject(
            HandlerCreationInjectable,
            new(serviceProvider,
                conquerorContextAccessor,
                messageIdFactory)) as TIHandler ?? throw new InvalidOperationException("could not create handler proxy");
    }

    private readonly record struct InjectableArg(
        IServiceProvider ServiceProvider,
        IConquerorContextAccessor ConquerorContextAccessor,
        IMessageIdFactory MessageIdFactory);

    private sealed class Injectable : ICoreMessageHandlerTypesInjectable<InjectableArg, object>
    {
        object ICoreMessageHandlerTypesInjectable<InjectableArg, object>
            .WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy>(InjectableArg arg)
        {
            var dispatcher = new MessageDispatcher<TMessage, TResponse>(
                arg.ServiceProvider,
                arg.ConquerorContextAccessor,
                arg.MessageIdFactory,
                MessageSenderFactory<TMessage, TResponse>.InProcess,
                null,
                MessageTransportRole.Sender,
                null);

            return new TProxy
            {
                Dispatcher = dispatcher,
            };
        }
    }
}
