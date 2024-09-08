using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipeline
{
    private readonly IConquerorContext conquerorContext;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares;

    public CommandPipeline(IConquerorContext conquerorContext,
                           List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares)
    {
        this.conquerorContext = conquerorContext;
        this.middlewares = middlewares;
    }

    public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                              TCommand initialCommand,
                                                              CommandTransportClientFactory transportClientFactory,
                                                              CancellationToken cancellationToken)
        where TCommand : class
    {
        var transportClient = await transportClientFactory.Create(typeof(TCommand), serviceProvider).ConfigureAwait(false);
        return await ExecuteNextMiddleware(0, initialCommand, conquerorContext, cancellationToken).ConfigureAwait(false);

        async Task<TResponse> ExecuteNextMiddleware(int index, TCommand command, IConquerorContext ctx, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                return await transportClient.ExecuteCommand<TCommand, TResponse>(command, serviceProvider, token).ConfigureAwait(false);
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            return await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
        }
    }
}
