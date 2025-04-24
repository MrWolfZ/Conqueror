// ReSharper disable once CheckNamespace

namespace Conqueror;

public interface IMessageSenders
{
    TIHandler For<TMessage, TResponse, TIHandler>(MessageTypes<TMessage, TResponse, TIHandler> messageTypes)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>;
}
