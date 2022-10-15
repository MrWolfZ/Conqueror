using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandPipeline
    {
        private readonly CommandContextAccessor commandContextAccessor;
        private readonly ConquerorContextAccessor conquerorContextAccessor;
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares;

        public CommandPipeline(CommandContextAccessor commandContextAccessor,
                               ConquerorContextAccessor conquerorContextAccessor,
                               IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares)
        {
            this.commandContextAccessor = commandContextAccessor;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.middlewares = middlewares.ToList();
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                  CommandHandlerMetadata metadata,
                                                                  TCommand initialCommand,
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
                    var handler = serviceProvider.GetRequiredService(metadata.HandlerType);

                    if (handler is ICommandHandler<TCommand, TResponse> h)
                    {
                        var responseFromHandler = await h.ExecuteCommand(command, token);
                        commandContext.SetResponse(responseFromHandler!);
                        return responseFromHandler;
                    }

                    await ((ICommandHandler<TCommand>)handler).ExecuteCommand(command, token);
                    commandContext.SetResponse(UnitCommandResponse.Instance);
                    return (TResponse)(object)UnitCommandResponse.Instance;
                }

                var (middlewareType, middlewareConfiguration) = middlewares[index];
                var invokerType = middlewareConfiguration is null ? typeof(CommandMiddlewareInvoker) : typeof(CommandMiddlewareInvoker<>).MakeGenericType(middlewareConfiguration.GetType());
                var invoker = (ICommandMiddlewareInvoker)Activator.CreateInstance(invokerType)!;

                var response = await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, t), middlewareType, middlewareConfiguration, serviceProvider, token);
                commandContext.SetResponse(response!);
                return response;
            }
        }
    }
}
