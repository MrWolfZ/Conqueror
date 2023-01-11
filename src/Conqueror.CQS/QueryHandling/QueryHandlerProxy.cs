using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryHandlerProxy<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : class
    {
        private readonly Action<IQueryPipelineBuilder>? configurePipeline;
        private readonly IServiceProvider serviceProvider;
        private readonly Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory;

        public QueryHandlerProxy(IServiceProvider serviceProvider, Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory, Action<IQueryPipelineBuilder>? configurePipeline)
        {
            this.serviceProvider = serviceProvider;
            this.transportClientFactory = transportClientFactory;
            this.configurePipeline = configurePipeline;
        }

        public Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default)
        {
            var pipelineBuilder = new QueryPipelineBuilder(serviceProvider);

            configurePipeline?.Invoke(pipelineBuilder);

            var pipeline = pipelineBuilder.Build();

            return pipeline.Execute<TQuery, TResponse>(serviceProvider, query, transportClientFactory, cancellationToken);
        }
    }
}
