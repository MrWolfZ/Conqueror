namespace Conqueror;

public static class InProcessMessageSenderBuilderExtensions
{
    public static IMessageSender<TMessage, TResponse> UseInProcess<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        if (
            builder.ServiceProvider.GetService(typeof(IInProcessMessageSenderFactory))
            is not IInProcessMessageSenderFactory senderFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IInProcessMessageSenderFactory)}'; did you forget to add Conqueror to the service collection?"
            );
        }

        return senderFactory.Get<TMessage, TResponse>();
    }

    public static IMessageSender<TMessage, TResponse>? UseInProcessIfAvailable<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        if (
            builder.ServiceProvider.GetService(typeof(IInProcessMessageSenderFactory))
            is not IInProcessMessageSenderFactory senderFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IInProcessMessageSenderFactory)}'; did you forget to add Conqueror to the service collection?"
            );
        }

        return senderFactory.GetIfAvailable<TMessage, TResponse>();
    }
}
