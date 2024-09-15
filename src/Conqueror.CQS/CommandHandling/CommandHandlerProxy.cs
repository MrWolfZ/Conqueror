using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerProxy<TCommand, TResponse>(
    IServiceProvider serviceProvider,
    CommandTransportClientFactory transportClientFactory,
    Action<ICommandPipeline<TCommand, TResponse>>? configurePipeline,
    CommandTransportRole transportRole)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : class
{
    public async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var transportTypeName = conquerorContext.GetExecutionTransportTypeName();
        if (transportTypeName is null || conquerorContext.GetCommandId() is null)
        {
            conquerorContext.SetCommandId(ActivitySpanId.CreateRandom().ToString());
        }

        var transportClient = await transportClientFactory.Create(typeof(TCommand), typeof(TResponse), serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new CommandTransportType(transportTypeName ?? transportClient.TransportTypeName, transportRole);

        var pipelineBuilder = new CommandPipeline<TCommand, TResponse>(serviceProvider, conquerorContext, transportType);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return await pipeline.Execute(serviceProvider,
                                      command,
                                      transportClient,
                                      transportType,
                                      cancellationToken)
                             .ConfigureAwait(false);
    }

    public ICommandHandler<TCommand, TResponse> WithPipeline(Action<ICommandPipeline<TCommand, TResponse>> configure)
    {
        if (configurePipeline is not null)
        {
            var originalConfigure = configure;
            configure = pipeline =>
            {
                originalConfigure(pipeline);
                configurePipeline(pipeline);
            };
        }

        return new CommandHandlerProxy<TCommand, TResponse>(serviceProvider,
                                                            transportClientFactory,
                                                            configure,
                                                            transportRole);
    }
}
