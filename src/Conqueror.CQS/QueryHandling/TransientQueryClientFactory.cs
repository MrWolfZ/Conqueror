using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class TransientQueryClientFactory : IQueryClientFactory
{
    private readonly QueryClientFactory innerFactory;
    private readonly IServiceProvider serviceProvider;

    public TransientQueryClientFactory(QueryClientFactory innerFactory, IServiceProvider serviceProvider)
    {
        this.innerFactory = innerFactory;
        this.serviceProvider = serviceProvider;
    }

    public THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        return innerFactory.CreateQueryClient<THandler>(serviceProvider, transportClientFactory);
    }
}
