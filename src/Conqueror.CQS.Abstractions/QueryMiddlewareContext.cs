using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class QueryMiddlewareContext<TQuery, TResponse>
    where TQuery : class
{
    public abstract TQuery Query { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract QueryTransportType TransportType { get; }

    public abstract Task<TResponse> Next(TQuery query, CancellationToken cancellationToken);
}
