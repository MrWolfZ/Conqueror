using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipelineRunner
{
    private readonly IConquerorContext conquerorContext;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares;

    public CommandPipelineRunner(IConquerorContext conquerorContext,
                           List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares)
    {
        this.conquerorContext = conquerorContext;
        this.middlewares = middlewares.AsEnumerable().Reverse().ToList();
    }

    public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                              TCommand initialCommand,
                                                              CommandTransportClientFactory transportClientFactory,
                                                              string? transportTypeName,
                                                              CancellationToken cancellationToken)
        where TCommand : class
    {
        var transportClient = await transportClientFactory.Create(typeof(TCommand), typeof(TResponse), serviceProvider).ConfigureAwait(false);
        var transportType = transportClient.TransportType with { Name = transportTypeName ?? transportClient.TransportType.Name };

        var next = (TCommand command, CancellationToken token) => transportClient.ExecuteCommand<TCommand, TResponse>(command, serviceProvider, token);

        foreach (var (_, middlewareConfiguration, invoker) in middlewares)
        {
            var nextToCall = next;
            next = (command, token) => invoker.Invoke(command,
                                                    (q, t) => nextToCall(q, t),
                                                    middlewareConfiguration,
                                                    serviceProvider,
                                                    conquerorContext,
                                                    transportType,
                                                    token);
        }

        return await next(initialCommand, cancellationToken).ConfigureAwait(false);
    }
}
