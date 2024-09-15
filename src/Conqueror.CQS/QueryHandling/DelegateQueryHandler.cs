using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class DelegateQueryHandler<TQuery, TResponse>(
    Func<TQuery, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
    IServiceProvider serviceProvider)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : class
{
    public Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default)
    {
        return handlerFn(query, serviceProvider, cancellationToken);
    }
}
