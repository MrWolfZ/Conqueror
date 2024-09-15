using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal delegate Task<TResponse> QueryMiddlewareNext<in TQuery, TResponse>(TQuery query, CancellationToken cancellationToken);

internal sealed class DefaultQueryMiddlewareContext<TQuery, TResponse>(
    TQuery query,
    QueryMiddlewareNext<TQuery, TResponse> next,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    QueryTransportType transportType,
    CancellationToken cancellationToken)
    : QueryMiddlewareContext<TQuery, TResponse>
    where TQuery : class
{
    public override TQuery Query { get; } = query;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override QueryTransportType TransportType { get; } = transportType;

    public override Task<TResponse> Next(TQuery query, CancellationToken cancellationToken) => next(query, cancellationToken);
}
