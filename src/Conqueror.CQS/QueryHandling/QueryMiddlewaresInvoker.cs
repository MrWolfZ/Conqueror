using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewaresInvoker
    {
        private readonly Dictionary<Type, Action<IQueryPipelineBuilder>> pipelineConfigurationMethodByHandlerType;

        public QueryMiddlewaresInvoker(IEnumerable<QueryHandlerPipelineConfiguration> configurations)
        {
            pipelineConfigurationMethodByHandlerType = configurations.ToDictionary(c => c.HandlerType, c => c.Configure);
        }

        public async Task<TResponse> InvokeMiddlewares<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                          QueryHandlerMetadata metadata,
                                                                          TQuery query,
                                                                          CancellationToken cancellationToken)
            where TQuery : class
        {
            var pipelineBuilder = new QueryPipelineBuilder(serviceProvider);

            if (pipelineConfigurationMethodByHandlerType.TryGetValue(metadata.HandlerType, out var configurationMethod))
            {
                configurationMethod(pipelineBuilder);
            }

            var pipeline = pipelineBuilder.Build();

            return await pipeline.Execute<TQuery, TResponse>(serviceProvider, metadata, query, cancellationToken);
        }
    }
}
