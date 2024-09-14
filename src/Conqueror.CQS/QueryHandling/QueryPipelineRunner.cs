using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipelineRunner
{
    private readonly IConquerorContext conquerorContext;
    private readonly List<IQueryMiddleware> middlewares;

    public QueryPipelineRunner(IConquerorContext conquerorContext,
                               List<IQueryMiddleware> middlewares)
    {
        this.conquerorContext = conquerorContext;
        this.middlewares = middlewares.AsEnumerable().Reverse().ToList();
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                            TQuery initialQuery,
                                                            QueryTransportClientFactory transportClientFactory,
                                                            string? transportTypeName,
                                                            CancellationToken cancellationToken)
        where TQuery : class
    {
        var transportClient = await transportClientFactory.Create(typeof(TQuery), typeof(TResponse), serviceProvider).ConfigureAwait(false);
        var transportRole = transportTypeName is null ? transportClient.TransportType.Role : QueryTransportRole.Server;
        var transportType = new QueryTransportType(transportTypeName ?? transportClient.TransportType.Name, transportRole);

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
