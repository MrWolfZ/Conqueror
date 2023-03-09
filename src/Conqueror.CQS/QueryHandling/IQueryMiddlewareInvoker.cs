using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal interface IQueryMiddlewareInvoker
{
    Type MiddlewareType { get; }

    Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                              QueryMiddlewareNext<TQuery, TResponse> next,
                                              object? middlewareConfiguration,
                                              IServiceProvider serviceProvider,
                                              IConquerorContext conquerorContext,
                                              CancellationToken cancellationToken)
        where TQuery : class;
}
