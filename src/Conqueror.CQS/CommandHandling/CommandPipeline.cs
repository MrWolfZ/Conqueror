using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandPipeline
    {
        private readonly CommandContextAccessor commandContextAccessor;
        private readonly ConquerorContextAccessor conquerorContextAccessor;
        private readonly List<(object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares;

        public CommandPipeline(CommandContextAccessor commandContextAccessor,
                               ConquerorContextAccessor conquerorContextAccessor,
                               IEnumerable<(object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares)
        {
            this.commandContextAccessor = commandContextAccessor;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.middlewares = middlewares.ToList();
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                  TCommand initialCommand,
                                                                  ICommandTransport transport,
                                                                  CancellationToken cancellationToken)
            where TCommand : class
        {
            var commandContext = new DefaultCommandContext(initialCommand);

            commandContextAccessor.CommandContext = commandContext;

            using var conquerorContext = conquerorContextAccessor.GetOrCreate();

            var finalResponse = await ExecuteNextMiddleware(0, initialCommand, cancellationToken);

            commandContextAccessor.ClearContext();

            return finalResponse;

            async Task<TResponse> ExecuteNextMiddleware(int index, TCommand command, CancellationToken token)
            {
                commandContext.SetCommand(command);

                if (index >= middlewares.Count)
                {
                    var responseFromHandler = await transport.Execute<TCommand, TResponse>(command, token);
                    commandContext.SetResponse(responseFromHandler!);
                    return responseFromHandler;
                }

                var (middlewareConfiguration, invoker) = middlewares[index];
                var response = await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, t), middlewareConfiguration, serviceProvider, token);
                commandContext.SetResponse(response!);
                return response;
            }
        }
    }
}
