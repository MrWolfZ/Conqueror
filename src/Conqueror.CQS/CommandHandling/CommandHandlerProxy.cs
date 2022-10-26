using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerProxy<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : class
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ICommandTransport transport;
        private readonly Action<ICommandPipelineBuilder>? configurePipeline;

        public CommandHandlerProxy(IServiceProvider serviceProvider, ICommandTransport transport, Action<ICommandPipelineBuilder>? configurePipeline)
        {
            this.serviceProvider = serviceProvider;
            this.transport = transport;
            this.configurePipeline = configurePipeline;
        }

        public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var pipelineBuilder = new CommandPipelineBuilder(serviceProvider);
            
            configurePipeline?.Invoke(pipelineBuilder);

            var pipeline = pipelineBuilder.Build();

            return pipeline.Execute<TCommand, TResponse>(serviceProvider, command, transport, cancellationToken);
        }
    }
}
