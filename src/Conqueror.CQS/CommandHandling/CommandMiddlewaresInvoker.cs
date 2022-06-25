using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewaresInvoker
    {
        private readonly Dictionary<Type, Action<ICommandPipelineBuilder>> pipelineConfigurationMethodByHandlerType;
        
        public CommandMiddlewaresInvoker(IEnumerable<CommandHandlerPipelineConfiguration> configurations)
        {
            pipelineConfigurationMethodByHandlerType = configurations.ToDictionary(c => c.HandlerType, c => c.Configure);
        }

        public async Task<TResponse> InvokeMiddlewares<TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                            CommandHandlerMetadata metadata,
                                                                            TCommand command,
                                                                            CancellationToken cancellationToken)
            where TCommand : class
        {
            var pipelineBuilder = new CommandPipelineBuilder(serviceProvider);

            if (pipelineConfigurationMethodByHandlerType.TryGetValue(metadata.HandlerType, out var configurationMethod))
            {
                configurationMethod(pipelineBuilder);
            }

            var pipeline = pipelineBuilder.Build();

            return await pipeline.Execute<TCommand, TResponse>(serviceProvider, metadata, command, cancellationToken);
        }

        public Task InvokeMiddlewares<TCommand>(IServiceProvider serviceProvider,
                                                CommandHandlerMetadata metadata,
                                                TCommand command,
                                                CancellationToken cancellationToken)
            where TCommand : class
        {
            return InvokeMiddlewares<TCommand, UnitCommandResponse>(serviceProvider, metadata, command, cancellationToken);
        }
    }
}
