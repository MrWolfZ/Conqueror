// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageClients
{
    IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>;

    IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(MessageTypes<TMessage, TResponse> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>;

    IMessageHandler<TMessage> For<TMessage>()
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>;

    IMessageHandler<TMessage> For<TMessage>(MessageTypes<TMessage, UnitMessageResponse> messageTypes)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>;
}
