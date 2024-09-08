using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipeline
{
    private readonly IConquerorContext conquerorContext;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares;

    public QueryPipeline(IConquerorContext conquerorContext,
                         List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares)
    {
        this.conquerorContext = conquerorContext;
        this.middlewares = middlewares;
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                            TQuery initialQuery,
                                                            QueryTransportClientFactory transportClientFactory,
                                                            CancellationToken cancellationToken)
        where TQuery : class
    {
        var transportClient = await transportClientFactory.Create(typeof(TQuery), serviceProvider).ConfigureAwait(false);
        return await ExecuteNextMiddleware(0, initialQuery, conquerorContext, cancellationToken).ConfigureAwait(false);

        async Task<TResponse> ExecuteNextMiddleware(int index, TQuery query, IConquerorContext ctx, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                return await transportClient.ExecuteQuery<TQuery, TResponse>(query, serviceProvider, token).ConfigureAwait(false);
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            return await invoker.Invoke(query, (q, t) => ExecuteNextMiddleware(index + 1, q, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
        }
    }
}
