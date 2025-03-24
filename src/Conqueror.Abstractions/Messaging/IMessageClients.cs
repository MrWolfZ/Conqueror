// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageClients
{
    THandler For<THandler>()
        where THandler : class, IGeneratedMessageHandler;

    public IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(IMessage<TMessage, TResponse> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>;

    public IMessageHandler<TMessage, UnitMessageResponse> For<TMessage>(IMessage<TMessage, UnitMessageResponse> messageTypes)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>;

    IMessageHandler<TMessage, TResponse> ForMessageType<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>;

    IMessageHandler<TMessage> ForMessageType<TMessage>()
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>;
}
