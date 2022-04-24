using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewaresInvoker
    {
        public async Task<TResponse> InvokeMiddlewares<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                            CommandHandlerMetadata metadata,
                                                                            TCommand command,
                                                                            CancellationToken cancellationToken)
            where TCommand : class
        {
            var attributes = metadata.MiddlewareConfigurationAttributes.ToList();

            return await ExecuteNextMiddleware(0, command, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(int index, TCommand cmd, CancellationToken token)
            {
                if (index >= attributes.Count)
                {
                    var handler = serviceProvider.GetRequiredService(metadata.HandlerType);

                    if (typeof(TResponse) == typeof(UnitCommandResponse))
                    {
                        handler = new Adapter<TCommand>((ICommandHandler<TCommand>)handler);
                    }
                    
                    return await ((ICommandHandler<TCommand, TResponse>)handler).ExecuteCommand(cmd, token);
                }
                
                var attribute = attributes[index];
                var invoker = (ICommandMiddlewareInvoker)serviceProvider.GetService(typeof(CommandMiddlewareInvoker<>).MakeGenericType(attribute.GetType()))!;

                return await invoker.Invoke(cmd, (c, t) => ExecuteNextMiddleware(index + 1, c, t), metadata, attribute, serviceProvider, token);
            }
        }

        public Task InvokeMiddlewares<TCommand>(IServiceProvider serviceProvider,
                                                CommandHandlerMetadata metadata,
                                                TCommand command,
                                                CancellationToken cancellationToken)
            where TCommand : class
        {
            return InvokeMiddlewares<TCommand, UnitCommandResponse>(serviceProvider, metadata, command, cancellationToken);
        }

        private sealed class Adapter<TCommand> : ICommandHandler<TCommand, UnitCommandResponse>
            where TCommand : class
        {
            private readonly ICommandHandler<TCommand> commandHandler;

            public Adapter(ICommandHandler<TCommand> commandHandler)
            {
                this.commandHandler = commandHandler;
            }

            public async Task<UnitCommandResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
            {
                await commandHandler.ExecuteCommand(command, cancellationToken);
                return UnitCommandResponse.Instance;
            }
        }
    }
}
