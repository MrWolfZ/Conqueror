using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipelineRunner<TQuery, TResponse>(
    ConquerorContext conquerorContext,
    List<IQueryMiddleware<TQuery, TResponse>> middlewares)
    where TQuery : class
{
    private readonly List<IQueryMiddleware<TQuery, TResponse>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public async Task<TResponse> Execute(IServiceProvider serviceProvider,
                                         TQuery initialQuery,
                                         IQueryTransportClient transportClient,
                                         QueryTransportType transportType,
                                         CancellationToken cancellationToken)
    {
        var next = (TQuery query, CancellationToken token) => transportClient.ExecuteQuery<TQuery, TResponse>(query, serviceProvider, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (query, token) =>
            {
                var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse>(query,
                                                                               (q, t) => nextToCall(q, t),
                                                                               serviceProvider,
                                                                               conquerorContext,
                                                                               transportType,
                                                                               token);

                return middleware.Execute(ctx);
            };
        }

        return await next(initialQuery, cancellationToken).ConfigureAwait(false);
    }
}
