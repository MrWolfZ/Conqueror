using System;
using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryClientFactory
{
    THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        return CreateQueryClient<THandler>(b => Task.FromResult(transportClientFactory(b)));
    }

    THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler;
}
