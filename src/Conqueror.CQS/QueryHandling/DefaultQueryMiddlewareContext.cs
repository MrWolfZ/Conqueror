using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal delegate Task<TResponse> QueryMiddlewareNext<in TQuery, TResponse>(TQuery query, CancellationToken cancellationToken);

internal sealed class DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration> : QueryMiddlewareContext<TQuery, TResponse, TConfiguration>
    where TQuery : class
{
    private readonly QueryMiddlewareNext<TQuery, TResponse> next;

    public DefaultQueryMiddlewareContext(TQuery query,
                                         QueryMiddlewareNext<TQuery, TResponse> next,
                                         TConfiguration configuration,
                                         IServiceProvider serviceProvider,
                                         CancellationToken cancellationToken)
    {
        this.next = next;
        Query = query;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
    }

    public override TQuery Query { get; }

    public override CancellationToken CancellationToken { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override TConfiguration Configuration { get; }

    public override Task<TResponse> Next(TQuery query, CancellationToken cancellationToken) => next(query, cancellationToken);
}
