using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryHandlerProxy<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : class
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IQueryTransportClient transportClient;
        private readonly Action<IQueryPipelineBuilder>? configurePipeline;

        public QueryHandlerProxy(IServiceProvider serviceProvider, IQueryTransportClient transportClient, Action<IQueryPipelineBuilder>? configurePipeline)
        {
            this.serviceProvider = serviceProvider;
            this.transportClient = transportClient;
            this.configurePipeline = configurePipeline;
        }

        public Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken)
        {
            var pipelineBuilder = new QueryPipelineBuilder(serviceProvider);
            
            configurePipeline?.Invoke(pipelineBuilder);

            var pipeline = pipelineBuilder.Build();

            return pipeline.Execute<TQuery, TResponse>(serviceProvider, query, transportClient, cancellationToken);
        }
    }
}
