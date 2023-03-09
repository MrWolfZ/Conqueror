using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipeline
{
    private readonly IConquerorContextAccessor conquerorContextAccessor;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares;
    private readonly QueryContextAccessor queryContextAccessor;

    public QueryPipeline(QueryContextAccessor queryContextAccessor,
                         IConquerorContextAccessor conquerorContextAccessor,
                         List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares)
    {
        this.queryContextAccessor = queryContextAccessor;
        this.conquerorContextAccessor = conquerorContextAccessor;
        this.middlewares = middlewares;
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                            TQuery initialQuery,
                                                            Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory,
                                                            CancellationToken cancellationToken)
        where TQuery : class
    {
        var queryId = queryContextAccessor.DrainExternalQueryId() ?? Guid.NewGuid().ToString("N");

        var queryContext = new DefaultQueryContext(initialQuery, queryId);

        queryContextAccessor.QueryContext = queryContext;

        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

        var transportClientBuilder = new QueryTransportClientBuilder(serviceProvider, typeof(TQuery));

        try
        {
            return await ExecuteNextMiddleware(0, initialQuery, conquerorContext, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            queryContextAccessor.ClearContext();
        }

        async Task<TResponse> ExecuteNextMiddleware(int index, TQuery query, IConquerorContext ctx, CancellationToken token)
        {
            queryContext.SetQuery(query);

            if (index >= middlewares.Count)
            {
                var transportClient = await transportClientFactory(transportClientBuilder).ConfigureAwait(false);
                var responseFromHandler = await transportClient.ExecuteQuery<TQuery, TResponse>(query, token).ConfigureAwait(false);
                queryContext.SetResponse(responseFromHandler);
                return responseFromHandler;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            var response = await invoker.Invoke(query, (q, t) => ExecuteNextMiddleware(index + 1, q, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
            queryContext.SetResponse(response);
            return response;
        }
    }
}
