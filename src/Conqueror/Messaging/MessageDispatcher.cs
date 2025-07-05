using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageDispatcher(
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory,
    MessageTransportRole transportRole)
    : IMessageDispatcher
{
    public async Task<TResponse> Dispatch<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        IMessagePipeline<TMessage, TResponse> pipeline,
        IMessageSender<TMessage, TResponse>? sender,
        ConfigureMessageSender<TMessage, TResponse>? configureSender,
        ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync,
        CancellationToken cancellationToken)
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
            var transportBuilder = new MessageSenderBuilder<TMessage, TResponse>(serviceProvider, conquerorContext);

            if (configureSender is not null)
            {
                sender = configureSender(transportBuilder);
            }
            else if (configureSenderAsync is not null)
            {
                sender = await configureSenderAsync(transportBuilder).ConfigureAwait(false);
            }
            else
            {
                sender = transportBuilder.UseInProcess();
            }
        }

        var transportType = new MessageTransportType(sender.TransportTypeName, transportRole);

        return await ((MessagePipeline<TMessage, TResponse>)pipeline).Execute(
                                                                         serviceProvider,
                                                                         message,
                                                                         sender,
                                                                         transportType,
                                                                         conquerorContext,
                                                                         cancellationToken)
                                                                     .ConfigureAwait(false);
    }
}
