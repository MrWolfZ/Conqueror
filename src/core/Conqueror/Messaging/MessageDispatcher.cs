namespace Conqueror.Messaging;

internal sealed class MessageDispatcher(
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory,
    MessageTransportRole transportRole
) : IMessageDispatcher
{
    public Task<TResponse> Dispatch<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        IMessagePipeline<TMessage, TResponse> pipeline,
        IMessageSender<TMessage, TResponse>? sender,
        ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync,
        CancellationToken cancellationToken
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        // TODO: move pipeline modification logic here instead of the proxy so that we can always
        // act on the concrete pipeline type
        if (pipeline is not MessagePipeline<TMessage, TResponse> concretePipeline)
        {
            throw new ArgumentException("pipeline must be a concrete pipeline", nameof(pipeline));
        }

        return DispatchInner(
            message,
            serviceProvider,
            concretePipeline,
            sender,
            configureSenderAsync,
            cancellationToken
        );
    }

    private async Task<TResponse> DispatchInner<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        MessagePipeline<TMessage, TResponse> pipeline,
        IMessageSender<TMessage, TResponse>? sender,
        ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync,
        CancellationToken cancellationToken
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

        var originalMessageId = conquerorContext.MessageId;

        // ensure that a message ID is available; if we are in a send operation, make
        // sure to create a new message ID for this execution in any case
        if (originalMessageId is null || transportRole is MessageTransportRole.Sender)
        {
            conquerorContext.MessageId = messageIdFactory.GenerateId();
        }

        if (sender is null)
        {
            var transportBuilder = new MessageSenderBuilder<TMessage, TResponse>(serviceProvider);

            if (configureSenderAsync is not null)
            {
                sender = await configureSenderAsync(transportBuilder).ConfigureAwait(false);
            }
            else
            {
                sender = transportBuilder.UseInProcess();
            }
        }

        var transportType = new MessageTransportType(sender.TransportTypeName, transportRole);

        return await pipeline
            .Execute(serviceProvider, message, sender, transportType, conquerorContext, cancellationToken)
            .ConfigureAwait(false);
    }
}
