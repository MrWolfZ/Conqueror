namespace Conqueror;

public readonly record struct MessageSenderBuilder<TMessage, TResponse>(IServiceProvider ServiceProvider)
    where TMessage : class, IMessage<TMessage, TResponse>;
