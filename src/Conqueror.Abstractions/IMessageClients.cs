using System;
using System.Threading.Tasks;

namespace Conqueror;

public interface IMessageClients
{
    IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(MessageTypes<TMessage, TResponse> _)
        where TMessage : class, IMessage<TResponse>;

    IMessageHandler<TMessage> For<TMessage>(MessageTypes<TMessage> _)
        where TMessage : class, IMessage;

    IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(MessageTypes<TMessage, TResponse> _, Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
        where TMessage : class, IMessage<TResponse>;

    IMessageHandler<TMessage> For<TMessage>(MessageTypes<TMessage> _, Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
        where TMessage : class, IMessage;

    IMessageHandler<TMessage, TResponse> For<TMessage, TResponse>(MessageTypes<TMessage, TResponse> _, Func<IMessageTransportClientBuilder, Task<IMessageTransportClient>> transportClientFactory)
        where TMessage : class, IMessage<TResponse>;

    IMessageHandler<TMessage> For<TMessage>(MessageTypes<TMessage> _, Func<IMessageTransportClientBuilder, Task<IMessageTransportClient>> transportClientFactory)
        where TMessage : class, IMessage;
}
