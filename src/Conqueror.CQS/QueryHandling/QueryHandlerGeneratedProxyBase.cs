using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal abstract class QueryHandlerGeneratedProxyBase<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : class
{
    private readonly IQueryHandler<TQuery, TResponse> target;

    protected QueryHandlerGeneratedProxyBase(IQueryHandler<TQuery, TResponse> target)
    {
        this.target = target;
    }

    public Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default)
    {
        return target.ExecuteQuery(query, cancellationToken);
    }

    public IQueryHandler<TQuery, TResponse> WithPipeline(Action<IQueryPipelineBuilder> configure)
    {
        return target.WithPipeline(configure);
    }
}
