using System;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class TransientQueryClientFactory : IQueryClientFactory
    {
        private readonly QueryClientFactory innerFactory;
        private readonly IServiceProvider serviceProvider;

        public TransientQueryClientFactory(QueryClientFactory innerFactory, IServiceProvider serviceProvider)
        {
            this.innerFactory = innerFactory;
            this.serviceProvider = serviceProvider;
        }

        public THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, IQueryTransportClient> transportBuilderFn, Action<IQueryPipelineBuilder>? configurePipeline = null)
            where THandler : class, IQueryHandler
        {
            return innerFactory.CreateQueryClient<THandler>(serviceProvider, transportBuilderFn, configurePipeline);
        }
    }
}
