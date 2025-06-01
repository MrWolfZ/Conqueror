using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageDispatcher(
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory,
    MessageTransportRole transportRole,
    Type? handlerType)
    : IMessageDispatcher
{
    public async Task<TResponse> Dispatch<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline,
        IMessageSender<TMessage, TResponse>? sender,
        ConfigureMessageSender<TMessage, TResponse>? configureSender,
        ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

        var originalMessageId = conquerorContext.GetMessageId();

        // ensure that a message ID is available for the transport client factory
        if (originalMessageId is null)
        {
            conquerorContext.SetMessageId(messageIdFactory.GenerateId());
        }

        // if we are in a send operation, make sure to create a new message ID for this execution if
        // we were called from within the call context of another handler
        if (originalMessageId is not null && transportRole is MessageTransportRole.Sender)
        {
            conquerorContext.SetMessageId(messageIdFactory.GenerateId());
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

        var pipeline = new MessagePipeline<TMessage, TResponse>(
            handlerType,
            serviceProvider,
            conquerorContext,
            transportType);

        configurePipeline?.Invoke(pipeline);

        var pipelineRunner = pipeline.Build(conquerorContext);

        return await pipelineRunner.Execute(
                                       serviceProvider,
                                       message,
                                       sender,
                                       transportType,
                                       cancellationToken)
                                   .ConfigureAwait(false);
    }
}
