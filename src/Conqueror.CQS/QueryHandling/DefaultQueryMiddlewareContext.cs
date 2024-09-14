using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal delegate Task<TResponse> QueryMiddlewareNext<in TQuery, TResponse>(TQuery query, CancellationToken cancellationToken);

internal sealed class DefaultQueryMiddlewareContext<TQuery, TResponse> : QueryMiddlewareContext<TQuery, TResponse>
    where TQuery : class
{
    private readonly QueryMiddlewareNext<TQuery, TResponse> next;

    public DefaultQueryMiddlewareContext(TQuery query,
                                         QueryMiddlewareNext<TQuery, TResponse> next,
                                         IServiceProvider serviceProvider,
                                         IConquerorContext conquerorContext,
                                         QueryTransportType transportType,
                                         CancellationToken cancellationToken)
    {
        this.next = next;
        Query = query;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ConquerorContext = conquerorContext;
        TransportType = transportType;
    }

    public override TQuery Query { get; }

    public override CancellationToken CancellationToken { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override IConquerorContext ConquerorContext { get; }

    public override QueryTransportType TransportType { get; }

    public override Task<TResponse> Next(TQuery query, CancellationToken cancellationToken) => next(query, cancellationToken);
}
