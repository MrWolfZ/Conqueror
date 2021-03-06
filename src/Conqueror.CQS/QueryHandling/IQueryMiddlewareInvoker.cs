using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal delegate Task<TResponse> QueryMiddlewareNext<in TQuery, TResponse>(TQuery query, CancellationToken cancellationToken);

    internal interface IQueryMiddlewareInvoker
    {
        Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                  QueryMiddlewareNext<TQuery, TResponse> next,
                                                  Type middlewareType,
                                                  object? middlewareConfiguration,
                                                  IServiceProvider serviceProvider,
                                                  CancellationToken cancellationToken)
            where TQuery : class;
    }
}
