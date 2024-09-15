using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal abstract class QueryHandlerGeneratedProxyBase<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> target) : IQueryHandler<TQuery, TResponse>
    where TQuery : class
{
    public Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default)
    {
        return target.Handle(query, cancellationToken);
    }

    public IQueryHandler<TQuery, TResponse> WithPipeline(Action<IQueryPipeline<TQuery, TResponse>> configure)
    {
        return target.WithPipeline(configure);
    }
}
