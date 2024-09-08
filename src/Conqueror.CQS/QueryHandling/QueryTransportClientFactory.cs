using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryTransportClientFactory
{
    private readonly IQueryTransportClient? transportClient;
    private readonly Func<IQueryTransportClientBuilder, IQueryTransportClient>? syncTransportClientFactory;
    private readonly Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>>? asyncTransportClientFactory;

    public QueryTransportClientFactory(IQueryTransportClient transportClient)
    {
        this.transportClient = transportClient;
    }

    public QueryTransportClientFactory(Func<IQueryTransportClientBuilder, IQueryTransportClient>? syncTransportClientFactory)
    {
        this.syncTransportClientFactory = syncTransportClientFactory;
    }

    public QueryTransportClientFactory(Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>>? asyncTransportClientFactory)
    {
        this.asyncTransportClientFactory = asyncTransportClientFactory;
    }

    public Task<IQueryTransportClient> Create(Type queryType, Type responseType, IServiceProvider serviceProvider)
    {
        if (transportClient is not null)
        {
            return Task.FromResult(transportClient);
        }

        var transportBuilder = new QueryTransportClientBuilder(serviceProvider, queryType, responseType);

        if (syncTransportClientFactory is not null)
        {
            return Task.FromResult(syncTransportClientFactory.Invoke(transportBuilder));
        }

        if (asyncTransportClientFactory is not null)
        {
            return asyncTransportClientFactory.Invoke(transportBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create transport client for query type '{queryType.Name}' since it was not configured with a factory");
    }
}
