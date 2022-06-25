using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandPipeline
    {
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares;

        public CommandPipeline(IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares)
        {
            this.middlewares = middlewares.ToList();
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                  CommandHandlerMetadata metadata,
                                                                  TCommand initialCommand,
                                                                  CancellationToken cancellationToken)
            where TCommand : class
        {
            return await ExecuteNextMiddleware(0, initialCommand, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(int index, TCommand command, CancellationToken token)
            {
                if (index >= middlewares.Count)
                {
                    var handler = serviceProvider.GetRequiredService(metadata.HandlerType);

                    if (handler is ICommandHandler<TCommand, TResponse> h)
                    {
                        return await h.ExecuteCommand(command, token);
                    }

                    await ((ICommandHandler<TCommand>)handler).ExecuteCommand(command, token);
                    return (TResponse)(object)UnitCommandResponse.Instance;
                }

                var (middlewareType, middlewareConfiguration) = middlewares[index];
                var invokerType = middlewareConfiguration is null ? typeof(CommandMiddlewareInvoker) : typeof(CommandMiddlewareInvoker<>).MakeGenericType(middlewareConfiguration.GetType());
                var invoker = (ICommandMiddlewareInvoker)Activator.CreateInstance(invokerType)!;

                return await invoker.Invoke(command, (c, t) => ExecuteNextMiddleware(index + 1, c, t), middlewareType, middlewareConfiguration, serviceProvider, token);
            }
        }
    }
}
