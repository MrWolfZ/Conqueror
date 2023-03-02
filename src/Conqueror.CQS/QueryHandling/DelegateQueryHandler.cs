using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class DelegateQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : class
{
    private readonly Func<TQuery, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn;
    private readonly IServiceProvider serviceProvider;

    public DelegateQueryHandler(Func<TQuery, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
                                IServiceProvider serviceProvider)
    {
        this.handlerFn = handlerFn;
        this.serviceProvider = serviceProvider;
    }

    public Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default)
    {
        return handlerFn(query, serviceProvider, cancellationToken);
    }
}
