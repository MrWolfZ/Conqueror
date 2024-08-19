using System;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class StreamProducerTransportClientFactory
{
    private readonly Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>>? asyncTransportClientFactory;
    private readonly Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>? syncTransportClientFactory;
    private readonly IStreamProducerTransportClient? transportClient;

    public StreamProducerTransportClientFactory(IStreamProducerTransportClient transportClient)
    {
        this.transportClient = transportClient;
    }

    public StreamProducerTransportClientFactory(Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>? syncTransportClientFactory)
    {
        this.syncTransportClientFactory = syncTransportClientFactory;
    }

    public StreamProducerTransportClientFactory(Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>>? asyncTransportClientFactory)
    {
        this.asyncTransportClientFactory = asyncTransportClientFactory;
    }

    public Task<IStreamProducerTransportClient> Create(Type requestType, IServiceProvider serviceProvider)
    {
        if (transportClient is not null)
        {
            return Task.FromResult(transportClient);
        }

        var transportBuilder = new StreamProducerTransportClientBuilder(serviceProvider, requestType);

        if (syncTransportClientFactory is not null)
        {
            return Task.FromResult(syncTransportClientFactory.Invoke(transportBuilder));
        }

        if (asyncTransportClientFactory is not null)
        {
            return asyncTransportClientFactory.Invoke(transportBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create transport client for streaming request type '{requestType.Name}' since it was not configured with a factory");
    }
}
