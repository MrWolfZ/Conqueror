using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Messaging;

internal sealed class MessageHandlerProxy<TMessage, TResponse>(
    IServiceProvider serviceProvider,
    MessageTransportClientFactory<TMessage, TResponse> transportClientFactory,
    Action<IMessagePipeline<TMessage, TResponse>>? configurePipelineField,
    MessageTransportRole transportRole)
    : IConfigurableMessageHandler<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    public async Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var originalMessageId = conquerorContext.GetMessageId();

        // ensure that a message ID is available for the transport client factory
        if (originalMessageId is null)
        {
            conquerorContext.SetMessageId(ActivitySpanId.CreateRandom().ToString());
        }

        var transportClient = await transportClientFactory.Create(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new MessageTransportType(transportClient.TransportTypeName, transportRole);

        // if we are an in-process client, make sure to create a new message ID for this execution if
        // we were called from within the call context of another handler
        if (originalMessageId is not null && transportType.IsInProcess() && transportRole == MessageTransportRole.Client)
        {
            conquerorContext.SetMessageId(ActivitySpanId.CreateRandom().ToString());
        }

        var pipeline = new MessagePipeline<TMessage, TResponse>(serviceProvider, conquerorContext, transportType);

        configurePipelineField?.Invoke(pipeline);

        var pipelineRunner = pipeline.Build(conquerorContext);

        return await pipelineRunner.Execute(serviceProvider,
                                            message,
                                            transportClient,
                                            transportType,
                                            cancellationToken)
                                   .ConfigureAwait(false);
    }

    public IMessageHandler<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        => new MessageHandlerProxy<TMessage, TResponse>(
            serviceProvider,
            transportClientFactory,
            pipeline =>
            {
                configurePipelineField?.Invoke(pipeline);
                configurePipeline(pipeline);
            },
            transportRole);

    public IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClient<TMessage, TResponse> configureTransport)
        => new MessageHandlerProxy<TMessage, TResponse>(
            serviceProvider,
            new(configureTransport),
            configurePipelineField,
            transportRole);

    public IMessageHandler<TMessage, TResponse> WithTransport(ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport)
        => new MessageHandlerProxy<TMessage, TResponse>(
            serviceProvider,
            new(configureTransport),
            configurePipelineField,
            transportRole);
}
