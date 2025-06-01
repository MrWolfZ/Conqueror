using System;

namespace Conqueror.Messaging;

internal sealed class InProcessMessageSenderFactory(
    IServiceProvider serviceProvider,
    MessageHandlerRegistry registry)
    : IInProcessMessageSenderFactory
{
    public IMessageSender<TMessage, TResponse> Get<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var (handler, isDisabled) = GetInternal<TMessage, TResponse>();

        if (isDisabled)
        {
            throw new InvalidOperationException($"in-process transport is disabled for message type '{typeof(TMessage)}'");
        }

        return handler ?? throw new InvalidOperationException($"there is no handler registered for message type '{typeof(TMessage)}'");
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
        var invoker = registry.GetReceiverHandlerInvoker<TMessage, TResponse, ICoreMessageHandlerTypesInjector>();

        if (invoker is null)
        {
            return (null, false);
        }

        var receiver = new InProcessMessageReceiver<TMessage, TResponse>(serviceProvider);
        invoker.TypesInjector.ConfigureInProcessReceiver(receiver);

        if (!receiver.IsEnabled)
        {
            return (null, true);
        }

        return (new InProcessMessageSender<TMessage, TResponse>(invoker), false);
    }
}
