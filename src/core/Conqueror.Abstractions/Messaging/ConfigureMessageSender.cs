namespace Conqueror;

public delegate IMessageSender<TMessage, TResponse> ConfigureMessageSender<TMessage, TResponse>(
    MessageSenderBuilder<TMessage, TResponse> builder
)
    where TMessage : class, IMessage<TMessage, TResponse>;

public delegate ValueTask<IMessageSender<TMessage, TResponse>> ConfigureMessageSenderAsync<TMessage, TResponse>(
    MessageSenderBuilder<TMessage, TResponse> builder
)
    where TMessage : class, IMessage<TMessage, TResponse>;
