using System;
using System.Threading;
using System.Threading.Tasks;

// it makes sense for these classes to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerProxy<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : class
    {
        private readonly CommandMiddlewaresInvoker invoker;
        private readonly CommandHandlerRegistry registry;
        private readonly IServiceProvider serviceProvider;

        public CommandHandlerProxy(CommandHandlerRegistry registry, CommandMiddlewaresInvoker invoker, IServiceProvider serviceProvider)
        {
            this.registry = registry;
            this.invoker = invoker;
            this.serviceProvider = serviceProvider;
        }

        public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var metadata = registry.GetCommandHandlerMetadata<TCommand, TResponse>();
            return invoker.InvokeMiddlewares<TCommand, TResponse>(serviceProvider, metadata, command, cancellationToken);
        }
    }

    internal sealed class CommandHandlerProxy<TCommand> : ICommandHandler<TCommand>
        where TCommand : class
    {
        private readonly CommandMiddlewaresInvoker invoker;
        private readonly CommandHandlerRegistry registry;
        private readonly IServiceProvider serviceProvider;

        public CommandHandlerProxy(CommandHandlerRegistry registry, CommandMiddlewaresInvoker invoker, IServiceProvider serviceProvider)
        {
            this.registry = registry;
            this.invoker = invoker;
            this.serviceProvider = serviceProvider;
        }

        public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var metadata = registry.GetCommandHandlerMetadata<TCommand>();
            return invoker.InvokeMiddlewares(serviceProvider, metadata, command, cancellationToken);
        }
    }
}
