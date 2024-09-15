using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipelineRunner<TCommand, TResponse>(
    IConquerorContext conquerorContext,
    List<ICommandMiddleware<TCommand, TResponse>> middlewares)
    where TCommand : class
{
    private readonly List<ICommandMiddleware<TCommand, TResponse>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public async Task<TResponse> Execute(IServiceProvider serviceProvider,
                                         TCommand initialCommand,
                                         CommandTransportClientFactory transportClientFactory,
                                         string? transportTypeName,
                                         CancellationToken cancellationToken)
    {
        var transportClient = await transportClientFactory.Create(typeof(TCommand), typeof(TResponse), serviceProvider).ConfigureAwait(false);
        var transportType = transportClient.TransportType with { Name = transportTypeName ?? transportClient.TransportType.Name };

        var next = (TCommand command, CancellationToken token) => transportClient.ExecuteCommand<TCommand, TResponse>(command, serviceProvider, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (command, token) =>
            {
                var ctx = new DefaultCommandMiddlewareContext<TCommand, TResponse>(command,
                                                                                   (c, t) => nextToCall(c, t),
                                                                                   serviceProvider,
                                                                                   conquerorContext,
                                                                                   transportType,
                                                                                   token);

                return middleware.Execute(ctx);
            };
        }

        return await next(initialCommand, cancellationToken).ConfigureAwait(false);
    }
}
