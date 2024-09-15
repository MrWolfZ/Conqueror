using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class TransientQueryClientFactory(
    QueryClientFactory innerFactory,
    IServiceProvider serviceProvider)
    : IQueryClientFactory
{
    public THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        return innerFactory.CreateQueryClient<THandler>(serviceProvider, transportClientFactory);
    }
}
