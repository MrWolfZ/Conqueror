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

    public Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                        TCommand initialCommand,
                                                        Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory,
                                                        CancellationToken cancellationToken)
        where TCommand : class
    {
        var transportBuilder = new CommandTransportClientBuilder(serviceProvider, typeof(TCommand));

        return ExecuteNextMiddleware(0, initialCommand, conquerorContext, cancellationToken);

        async Task<TResponse> ExecuteNextMiddleware(int index, TCommand command, IConquerorContext ctx, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                var transport = await transportClientFactory(transportBuilder).ConfigureAwait(false);
                return await transport.ExecuteCommand<TCommand, TResponse>(command, token).ConfigureAwait(false);
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            return await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
        }
    }
}
