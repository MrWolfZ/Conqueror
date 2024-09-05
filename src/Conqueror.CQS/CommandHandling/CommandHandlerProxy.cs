using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerProxy<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : class
{
    private readonly CommandMiddlewareRegistry commandMiddlewareRegistry;
    private readonly Action<ICommandPipelineBuilder>? configurePipeline;
    private readonly IServiceProvider serviceProvider;
    private readonly CommandTransportClientFactory transportClientFactory;

    public CommandHandlerProxy(IServiceProvider serviceProvider,
                               CommandTransportClientFactory transportClientFactory,
                               Action<ICommandPipelineBuilder>? configurePipeline,
                               CommandMiddlewareRegistry commandMiddlewareRegistry)
    {
        this.serviceProvider = serviceProvider;
        this.transportClientFactory = transportClientFactory;
        this.configurePipeline = configurePipeline;
        this.commandMiddlewareRegistry = commandMiddlewareRegistry;
    }

    public async Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        if (!conquerorContext.IsExecutionFromTransport() || conquerorContext.GetCommandId() is null)
        {
            conquerorContext.SetCommandId(ActivitySpanId.CreateRandom().ToString());
        }

        var pipelineBuilder = new CommandPipelineBuilder(serviceProvider, commandMiddlewareRegistry);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return await pipeline.Execute<TCommand, TResponse>(serviceProvider, command, transportClientFactory, cancellationToken).ConfigureAwait(false);
    }

    public ICommandHandler<TCommand, TResponse> WithPipeline(Action<ICommandPipelineBuilder> configure)
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
                                                            commandMiddlewareRegistry);
    }
}
