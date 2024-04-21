using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryTransportClient
{
    Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                    IServiceProvider serviceProvider,
                                                    CancellationToken cancellationToken)
        where TQuery : class;
}
