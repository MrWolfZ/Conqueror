using System;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class TransientCommandClientFactory : ICommandClientFactory
    {
        private readonly CommandClientFactory innerFactory;
        private readonly IServiceProvider serviceProvider;

        public TransientCommandClientFactory(CommandClientFactory innerFactory, IServiceProvider serviceProvider)
        {
            this.innerFactory = innerFactory;
            this.serviceProvider = serviceProvider;
        }

        public THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, ICommandTransportClient> transportBuilderFn, Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler
        {
            return innerFactory.CreateCommandClient<THandler>(serviceProvider, transportBuilderFn, configurePipeline);
        }
    }
}
