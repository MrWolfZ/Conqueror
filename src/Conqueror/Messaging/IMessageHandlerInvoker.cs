namespace Conqueror.Messaging;

internal interface IMessageHandlerInvoker
{
    Task<TResponse> Invoke<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TMessage : class, IMessage<TMessage, TResponse>;
}
