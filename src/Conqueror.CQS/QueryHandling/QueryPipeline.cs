using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryPipeline
    {
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares;

        public QueryPipeline(IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares)
        {
            this.middlewares = middlewares.ToList();
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                QueryHandlerMetadata metadata,
                                                                TQuery initialQuery,
                                                                CancellationToken cancellationToken)
            where TQuery : class
        {
            return await ExecuteNextMiddleware(0, initialQuery, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(int index, TQuery query, CancellationToken token)
            {
                if (index >= middlewares.Count)
                {
                    var handler = (IQueryHandler<TQuery, TResponse>)serviceProvider.GetRequiredService(metadata.HandlerType);
                    return await handler.ExecuteQuery(query, token);
                }

                var (middlewareType, middlewareConfiguration) = middlewares[index];
                var invokerType = middlewareConfiguration is null ? typeof(QueryMiddlewareInvoker) : typeof(QueryMiddlewareInvoker<>).MakeGenericType(middlewareConfiguration.GetType());
                var invoker = (IQueryMiddlewareInvoker)Activator.CreateInstance(invokerType)!;

                return await invoker.Invoke(query, (q, t) => ExecuteNextMiddleware(index + 1, q, t), middlewareType, middlewareConfiguration, serviceProvider, token);
            }
        }
    }
}
