// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageClients
{
    THandler For<THandler>()
        where THandler : class, IGeneratedMessageHandler;

    IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(IMessageTypes<TMessage, TResponse> messageTypes)
        where TMessage : class, IMessage<TResponse>;

    IMessageHandler<TMessage, UnitMessageResponse> For<TMessage>(IMessageTypes<TMessage, UnitMessageResponse> messageTypes)
        where TMessage : class, IMessage<UnitMessageResponse>;

    IMessageHandler<TMessage, TResponse> ForMessageType<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>;

    IMessageHandler<TMessage> ForMessageType<TMessage>()
        where TMessage : class, IMessage<UnitMessageResponse>;
}
