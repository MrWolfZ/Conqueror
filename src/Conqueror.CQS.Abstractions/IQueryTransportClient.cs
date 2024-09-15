using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryTransportClient
{
    string TransportTypeName { get; }

    Task<TResponse> Send<TQuery, TResponse>(TQuery query,
                                            IServiceProvider serviceProvider,
                                            CancellationToken cancellationToken)
        where TQuery : class;
}
