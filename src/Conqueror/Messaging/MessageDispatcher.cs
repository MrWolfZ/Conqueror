using System;
using System.Diagnostics.CodeAnalysis;
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

        var initialCapacity = transportRole is MessageTransportRole.Sender
            ? PipelineCapacityCache<TMessage>.MaxObservedSenderPipelineCapacity
            : PipelineCapacityCache<TMessage>.MaxObservedHandlerPipelineCapacity;

        var pipeline = new MessagePipeline<TMessage, TResponse>(
            handlerType,
            message,
            serviceProvider,
            conquerorContext,
            transportType,
            initialCapacity);

        configurePipeline?.Invoke(pipeline);

        if (pipeline.Capacity > initialCapacity)
        {
            if (transportRole is MessageTransportRole.Sender)
            {
                PipelineCapacityCache<TMessage>.MaxObservedSenderPipelineCapacity = pipeline.Capacity;
            }
            else
            {
                PipelineCapacityCache<TMessage>.MaxObservedHandlerPipelineCapacity = pipeline.Capacity;
            }
        }

        return await pipeline.Execute(
                                 serviceProvider,
                                 message,
                                 sender,
                                 transportType,
                                 cancellationToken)
                             .ConfigureAwait(false);
    }
}

// performance optimization: we assume that most of the time the pipeline configuration will be very stable for a given
// message and transport role (e.g. the same middlewares will be used for sending or handling a message), so we cache the
// maximum observed pipeline size so that we can use it as the initial capacity for the pipeline; this way we avoid
// unnecessary resizes of the backing list; this performance optimization has been successfully validated through
// benchmarking, and it significantly reduces allocations for pipelines with more than 8 middlewares (which we assume
// is going to be fairly common)
[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "intentional design to leverage static classes as cache")]
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "used as static lookup key")]
file static class PipelineCapacityCache<TMessage>
{
    public static int MaxObservedSenderPipelineCapacity { get; set; }

    public static int MaxObservedHandlerPipelineCapacity { get; set; }
}
