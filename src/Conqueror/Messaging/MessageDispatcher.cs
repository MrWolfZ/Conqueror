using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Messaging;

internal sealed class MessageDispatcher<TMessage, TResponse>(
    IServiceProvider serviceProvider,
    MessageSenderFactory<TMessage, TResponse> senderFactory,
    Action<IMessagePipeline<TMessage, TResponse>>? configurePipelineField,
    MessageTransportRole transportRole)
    : IMessageDispatcher<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public async Task<TResponse> Dispatch(TMessage message, CancellationToken cancellationToken)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();
        var messageIdFactory = serviceProvider.GetRequiredService<IMessageIdFactory>();

        var originalMessageId = conquerorContext.GetMessageId();

        // ensure that a message ID is available for the transport client factory
        if (originalMessageId is null)
        {
            conquerorContext.SetMessageId(messageIdFactory.GenerateId());
        }

        var messageSender = await senderFactory.Create(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new MessageTransportType(messageSender.TransportTypeName, transportRole);

        // if we are an in-process client, make sure to create a new message ID for this execution if
        // we were called from within the call context of another handler
        if (originalMessageId is not null && transportType.IsInProcess() && transportRole == MessageTransportRole.Sender)
        {
            conquerorContext.SetMessageId(messageIdFactory.GenerateId());
        }

        var pipeline = new MessagePipeline<TMessage, TResponse>(serviceProvider, conquerorContext, transportType);

        configurePipelineField?.Invoke(pipeline);

        var pipelineRunner = pipeline.Build(conquerorContext);

        return await pipelineRunner.Execute(serviceProvider,
                                            message,
                                            messageSender,
                                            transportType,
                                            cancellationToken)
                                   .ConfigureAwait(false);
    }

    public IMessageDispatcher<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        => new MessageDispatcher<TMessage, TResponse>(
            serviceProvider,
            senderFactory,
            pipeline =>
            {
                configurePipelineField?.Invoke(pipeline);
                configurePipeline(pipeline);
            },
            transportRole);

    public IMessageDispatcher<TMessage, TResponse> WithSender(ConfigureMessageSender<TMessage, TResponse> configureSender)
        => new MessageDispatcher<TMessage, TResponse>(
            serviceProvider,
            new(configureSender),
            configurePipelineField,
            transportRole);

    public IMessageDispatcher<TMessage, TResponse> WithSender(ConfigureMessageSenderAsync<TMessage, TResponse> configureSender)
        => new MessageDispatcher<TMessage, TResponse>(
            serviceProvider,
            new(configureSender),
            configurePipelineField,
            transportRole);
}
