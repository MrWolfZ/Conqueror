namespace Conqueror.Messaging;

internal sealed class InProcessMessageSenderFactory(IServiceProvider serviceProvider, MessageHandlerRegistry registry)
    : IInProcessMessageSenderFactory
{
    private readonly ConcurrentDictionary<Type, object?> senderByMessageType = [];

    public IMessageSender<TMessage, TResponse> Get<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var (handler, isDisabled) = GetInternal<TMessage, TResponse>();

        if (isDisabled)
        {
            throw new InvalidOperationException(
                $"in-process transport is disabled for message type '{typeof(TMessage)}'"
            );
        }

        return handler
            ?? throw new InvalidOperationException(
                $"there is no handler registered for message type '{typeof(TMessage)}'"
            );
    }

    public IMessageSender<TMessage, TResponse>? GetIfAvailable<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var (handler, _) = GetInternal<TMessage, TResponse>();

        return handler;
    }

    private (IMessageSender<TMessage, TResponse>? Handler, bool IsDisabled) GetInternal<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        if (senderByMessageType.TryGetValue(typeof(TMessage), out var sender))
        {
            return sender is IMessageSender<TMessage, TResponse> s ? (s, false) : (null, true);
        }

        var invoker = registry.GetReceiverHandlerInvoker<TMessage, TResponse, ICoreMessageHandlerTypesInjector>();

        if (invoker is null)
        {
            return (null, false);
        }

        var receiver = new InProcessMessageReceiver<TMessage, TResponse>(serviceProvider);
        invoker.TypesInjector.ConfigureInProcessReceiver(receiver);

        if (!receiver.IsEnabled)
        {
            if (!receiver.MustBeConfiguredOnEveryMessage)
            {
                senderByMessageType[typeof(TMessage)] = null;
            }

            return (null, true);
        }

        var newSender = new InProcessMessageSender<TMessage, TResponse>(invoker);

        if (!receiver.MustBeConfiguredOnEveryMessage)
        {
            senderByMessageType[typeof(TMessage)] = newSender;
        }

        return (newSender, false);
    }
}
