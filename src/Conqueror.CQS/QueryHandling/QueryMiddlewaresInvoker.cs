using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewaresInvoker
    {
        private readonly ConcurrentDictionary<Type, Action<IQueryPipelineBuilder>> pipelineConfigurationMethodByHandlerType = new();

        public async Task<TResponse> InvokeMiddlewares<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                          QueryHandlerMetadata metadata,
                                                                          TQuery query,
                                                                          CancellationToken cancellationToken)
            where TQuery : class
        {
            var configurationMethod = pipelineConfigurationMethodByHandlerType.GetOrAdd(metadata.HandlerType, CreatePublishFunction);

            var pipelineBuilder = new QueryPipelineBuilder();

            configurationMethod(pipelineBuilder);

            var pipeline = pipelineBuilder.Build();

            return await pipeline.Execute<TQuery, TResponse>(serviceProvider, metadata, query, cancellationToken);
        }

        private static Action<IQueryPipelineBuilder> CreatePublishFunction(Type handlerType)
        {
            // TODO: validate signature
            var pipelineConfigurationMethod = handlerType.GetMethod("ConfigurePipeline", BindingFlags.Public | BindingFlags.Static);

            var builderParam = Expression.Parameter(typeof(IQueryPipelineBuilder));
            var body = pipelineConfigurationMethod is null ? (Expression)Expression.Block() : Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam);
            var compiled = lambda.Compile();
            return (Action<IQueryPipelineBuilder>)compiled;
        }
    }
}
