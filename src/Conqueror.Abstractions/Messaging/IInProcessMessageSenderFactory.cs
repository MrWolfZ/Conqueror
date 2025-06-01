// ReSharper disable once CheckNamespace

namespace Conqueror;

public interface IInProcessMessageSenderFactory
{
    IMessageSender<TMessage, TResponse> Get<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>;

    IMessageSender<TMessage, TResponse>? GetIfAvailable<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>;
}
