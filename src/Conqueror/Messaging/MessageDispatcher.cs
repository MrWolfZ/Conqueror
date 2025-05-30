using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageDispatcher<TMessage, TResponse>(
    IServiceProvider serviceProvider,
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory,
    MessageSenderFactory<TMessage, TResponse> senderFactory,
    Action<IMessagePipeline<TMessage, TResponse>>? configurePipelineField,
    MessageTransportRole transportRole,
    Type? handlerType)
    : IMessageDispatcher<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public async Task<TResponse> Dispatch(TMessage message, CancellationToken cancellationToken)
    {
        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

        var originalMessageId = conquerorContext.GetMessageId();

        // ensure that a message ID is available for the transport client factory
        if (originalMessageId is null)
        {
            conquerorContext.SetMessageId(messageIdFactory.GenerateId());
        }

        var messageSender = senderFactory.CreateSync(serviceProvider, conquerorContext)
                            ?? await senderFactory.CreateAsync(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new MessageTransportType(messageSender.TransportTypeName, transportRole);

        // if we are in a send operation, make sure to create a new message ID for this execution if
        // we were called from within the call context of another handler
        if (originalMessageId is not null && transportRole is MessageTransportRole.Sender)
        {
            conquerorContext.SetMessageId(messageIdFactory.GenerateId());
        }

        var middlewares = ArrayPool<IMessageMiddleware<TMessage, TResponse>>.Shared.Rent(128);

        try
        {
            var pipeline = new MessagePipeline<TMessage, TResponse>(
                handlerType,
                serviceProvider,
                conquerorContext,
                transportType,
                middlewares);

            configurePipelineField?.Invoke(pipeline);

            var pipelineRunner = pipeline.Build(conquerorContext);

            return await pipelineRunner.Execute(
                                           serviceProvider,
                                           message,
                                           messageSender,
                                           transportType,
                                           cancellationToken)
                                       .ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<IMessageMiddleware<TMessage, TResponse>>.Shared.Return(middlewares, true);
        }
    }

    public IMessageDispatcher<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        => new MessageDispatcher<TMessage, TResponse>(
            serviceProvider,
            conquerorContextAccessor,
            messageIdFactory,
            senderFactory,
            (Action<IMessagePipeline<TMessage, TResponse>>)Delegate.Combine(configurePipelineField, configurePipeline),
            transportRole,
            handlerType);

    public IMessageDispatcher<TMessage, TResponse> WithSender(ConfigureMessageSender<TMessage, TResponse> configureSender)
        => new MessageDispatcher<TMessage, TResponse>(
            serviceProvider,
            conquerorContextAccessor,
            messageIdFactory,
            new(configureSender),
            configurePipelineField,
            transportRole,
            handlerType);

    public IMessageDispatcher<TMessage, TResponse> WithSender(ConfigureMessageSenderAsync<TMessage, TResponse> configureSender)
        => new MessageDispatcher<TMessage, TResponse>(
            serviceProvider,
            conquerorContextAccessor,
            messageIdFactory,
            new(configureSender),
            configurePipelineField,
            transportRole,
            handlerType);
}
