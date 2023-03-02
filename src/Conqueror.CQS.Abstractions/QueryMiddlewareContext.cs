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

    public abstract Task<TResponse> Next(TQuery query, CancellationToken cancellationToken);
}

public abstract class QueryMiddlewareContext<TQuery, TResponse, TConfiguration> : QueryMiddlewareContext<TQuery, TResponse>
    where TQuery : class
{
    public abstract TConfiguration Configuration { get; }
}
