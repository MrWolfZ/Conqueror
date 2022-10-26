using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandHandlerProxy<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : class
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Func<ICommandTransportBuilder, ICommandTransport> transportFactory;
        private readonly Action<ICommandPipelineBuilder>? configurePipeline;

        public CommandHandlerProxy(IServiceProvider serviceProvider, Func<ICommandTransportBuilder, ICommandTransport> transportFactory, Action<ICommandPipelineBuilder>? configurePipeline)
        {
            this.serviceProvider = serviceProvider;
            this.transportFactory = transportFactory;
            this.configurePipeline = configurePipeline;
        }

        public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken)
        {
            var pipelineBuilder = new CommandPipelineBuilder(serviceProvider);
            
            configurePipeline?.Invoke(pipelineBuilder);

            var pipeline = pipelineBuilder.Build();

            return pipeline.Execute<TCommand, TResponse>(serviceProvider, command, transportFactory, cancellationToken);
        }
    }
}
