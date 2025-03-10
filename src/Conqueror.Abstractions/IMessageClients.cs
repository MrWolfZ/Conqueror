namespace Conqueror;

public interface IMessageClients
{
    THandler For<THandler>()
        where THandler : class, IMessageHandler, IGeneratedMessageHandler;

    IMessageHandler<TMessage, TResponse> ForMessageType<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>;

    IMessageHandler<TMessage> ForMessageType<TMessage>()
        where TMessage : class, IMessage<UnitMessageResponse>;
}
