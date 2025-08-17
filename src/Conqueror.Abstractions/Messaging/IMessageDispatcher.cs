namespace Conqueror;

internal interface IMessageDispatcher
{
    Task<TResponse> Dispatch<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        IMessagePipeline<TMessage, TResponse> pipeline,
        IMessageSender<TMessage, TResponse>? sender,
        ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync,
        CancellationToken cancellationToken
    )
        where TMessage : class, IMessage<TMessage, TResponse>;
}
