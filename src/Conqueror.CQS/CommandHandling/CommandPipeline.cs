using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipeline
{
    private readonly CommandContextAccessor commandContextAccessor;
    private readonly IConquerorContextAccessor conquerorContextAccessor;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares;

    public CommandPipeline(CommandContextAccessor commandContextAccessor,
                           IConquerorContextAccessor conquerorContextAccessor,
                           List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares)
    {
        this.commandContextAccessor = commandContextAccessor;
        this.conquerorContextAccessor = conquerorContextAccessor;
        this.middlewares = middlewares;
    }

    public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                              TCommand initialCommand,
                                                              Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory,
                                                              CancellationToken cancellationToken)
        where TCommand : class
    {
        var commandId = commandContextAccessor.DrainExternalCommandId() ?? Guid.NewGuid().ToString("N");

        var commandContext = new DefaultCommandContext(initialCommand, commandId);

        commandContextAccessor.CommandContext = commandContext;

        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

        var transportBuilder = new CommandTransportClientBuilder(serviceProvider, typeof(TCommand));

        try
        {
            return await ExecuteNextMiddleware(0, initialCommand, conquerorContext, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            commandContextAccessor.ClearContext();
        }

        async Task<TResponse> ExecuteNextMiddleware(int index, TCommand command, IConquerorContext ctx, CancellationToken token)
        {
            commandContext.SetCommand(command);

            if (index >= middlewares.Count)
            {
                var transport = await transportClientFactory(transportBuilder).ConfigureAwait(false);
                var responseFromHandler = await transport.ExecuteCommand<TCommand, TResponse>(command, token).ConfigureAwait(false);
                commandContext.SetResponse(responseFromHandler);
                return responseFromHandler;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            var response = await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
            commandContext.SetResponse(response);
            return response;
        }
    }
}
