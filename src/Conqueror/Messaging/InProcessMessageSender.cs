namespace Conqueror.Messaging;

internal sealed class InProcessMessageSender<TMessage, TResponse>(IMessageReceiverHandlerInvoker invoker)
    : IMessageSender<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public string TransportTypeName => ConquerorConstants.InProcessTransportName;

    public Task<TResponse> Send(
        TMessage message,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    ) => invoker.Invoke<TMessage, TResponse>(message, serviceProvider, TransportTypeName, cancellationToken);
}
