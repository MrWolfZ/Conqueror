using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror;

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

        var transportTypeName = conquerorContext.GetExecutionTransportTypeName();
        var isInProcessClient = transportTypeName is null && transportRole == MessageTransportRole.Client;
        if (conquerorContext.GetMessageId() is null || isInProcessClient)
        {
            conquerorContext.SetMessageId(ActivitySpanId.CreateRandom().ToString());
        }

        var transportClient = await transportClientFactory.Create(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new MessageTransportType(transportTypeName ?? transportClient.TransportTypeName, transportRole);

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
