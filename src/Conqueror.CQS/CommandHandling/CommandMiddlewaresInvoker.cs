using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewaresInvoker
    {
        public async Task<TResponse> InvokeMiddlewares<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                            ICommandHandler<TCommand, TResponse> handler,
                                                                            CommandHandlerMetadata metadata,
                                                                            TCommand command,
                                                                            CancellationToken cancellationToken)
            where TCommand : class
        {
            var index = 0;
            var attributes = metadata.MiddlewareConfigurationAttributes.ToList();

            return await ExecuteNextMiddleware(command, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(TCommand cmd, CancellationToken token)
            {
                if (index >= attributes.Count)
                {
                    return await handler.ExecuteCommand(cmd, token);
                }
                
                var attribute = attributes[index++];
                var invoker = (ICommandMiddlewareInvoker)serviceProvider.GetService(typeof(CommandMiddlewareInvoker<>).MakeGenericType(attribute.GetType()))!;

                return await invoker.Invoke(cmd, ExecuteNextMiddleware, metadata, attribute, serviceProvider, token);
            }
        }

        public Task InvokeMiddlewares<TCommand>(IServiceProvider serviceProvider,
                                                ICommandHandler<TCommand> handler,
                                                CommandHandlerMetadata metadata,
                                                TCommand command,
                                                CancellationToken cancellationToken)
            where TCommand : class
        {
            return InvokeMiddlewares(serviceProvider, new Adapter<TCommand>(handler), metadata, command, cancellationToken);
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
