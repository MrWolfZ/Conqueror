using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerProxy<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : class
    {
        private readonly IServiceProvider serviceProvider;
        private readonly CommandHandlerMetadata metadata;
        private readonly Action<ICommandPipelineBuilder>? configurePipeline;

        public CommandHandlerProxy(IServiceProvider serviceProvider, CommandHandlerMetadata metadata, Action<ICommandPipelineBuilder>? configurePipeline)
        {
            this.serviceProvider = serviceProvider;
            this.metadata = metadata;
            this.configurePipeline = configurePipeline;
        }

        public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var pipelineBuilder = new CommandPipelineBuilder(serviceProvider);
            
            configurePipeline?.Invoke(pipelineBuilder);

            var pipeline = pipelineBuilder.Build();

            return pipeline.Execute<TCommand, TResponse>(serviceProvider, metadata, command, cancellationToken);
        }
    }
}
