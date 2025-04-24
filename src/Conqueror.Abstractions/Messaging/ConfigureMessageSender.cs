using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate IMessageSender<TMessage, TResponse> ConfigureMessageSender<TMessage, TResponse>(
    IMessageSenderBuilder<TMessage, TResponse> builder)
    where TMessage : class, IMessage<TMessage, TResponse>;

public delegate Task<IMessageSender<TMessage, TResponse>> ConfigureMessageSenderAsync<TMessage, TResponse>(
    IMessageSenderBuilder<TMessage, TResponse> builder)
    where TMessage : class, IMessage<TMessage, TResponse>;
