using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipelineRunner
{
    private readonly IConquerorContext conquerorContext;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares;

    public QueryPipelineRunner(IConquerorContext conquerorContext,
                               List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares)
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
        var transportType = transportClient.TransportType with { Name = transportTypeName ?? transportClient.TransportType.Name };

        var next = (TQuery query, CancellationToken token) => transportClient.ExecuteQuery<TQuery, TResponse>(query, serviceProvider, token);

        foreach (var (_, middlewareConfiguration, invoker) in middlewares)
        {
            var nextToCall = next;
            next = (query, token) => invoker.Invoke(query,
                                                    (q, t) => nextToCall(q, t),
                                                    middlewareConfiguration,
                                                    serviceProvider,
                                                    conquerorContext,
                                                    transportType,
                                                    token);
        }

        return await next(initialQuery, cancellationToken).ConfigureAwait(false);
    }
}
