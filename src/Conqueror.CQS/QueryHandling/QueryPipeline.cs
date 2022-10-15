using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryPipeline
    {
        private readonly ConquerorContextAccessor conquerorContextAccessor;
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares;
        private readonly QueryContextAccessor queryContextAccessor;

        public QueryPipeline(QueryContextAccessor queryContextAccessor,
                             ConquerorContextAccessor conquerorContextAccessor,
                             IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares)
        {
            this.queryContextAccessor = queryContextAccessor;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.middlewares = middlewares.ToList();
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                QueryHandlerMetadata metadata,
                                                                TQuery initialQuery,
                                                                CancellationToken cancellationToken)
            where TQuery : class
        {
            var queryContext = new DefaultQueryContext(initialQuery);

            queryContextAccessor.QueryContext = queryContext;

            using var conquerorContext = conquerorContextAccessor.GetOrCreate();
            
            var finalResponse = await ExecuteNextMiddleware(0, initialQuery, cancellationToken);

            queryContextAccessor.ClearContext();

            return finalResponse;

            async Task<TResponse> ExecuteNextMiddleware(int index, TQuery query, CancellationToken token)
            {
                queryContext.SetQuery(query);

                if (index >= middlewares.Count)
                {
                    var handler = (IQueryHandler<TQuery, TResponse>)serviceProvider.GetRequiredService(metadata.HandlerType);
                    var responseFromHandler = await handler.ExecuteQuery(query, token);
                    queryContext.SetResponse(responseFromHandler!);
                    return responseFromHandler;
                }

                var (middlewareType, middlewareConfiguration) = middlewares[index];
                var invokerType = middlewareConfiguration is null ? typeof(QueryMiddlewareInvoker) : typeof(QueryMiddlewareInvoker<>).MakeGenericType(middlewareConfiguration.GetType());
                var invoker = (IQueryMiddlewareInvoker)Activator.CreateInstance(invokerType)!;

                var response = await invoker.Invoke(query, (q, t) => ExecuteNextMiddleware(index + 1, q, t), middlewareType, middlewareConfiguration, serviceProvider, token);
                queryContext.SetResponse(response!);
                return response;
            }
        }
    }
}
