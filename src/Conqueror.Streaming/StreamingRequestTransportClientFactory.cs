using System;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestTransportClientFactory
{
    private readonly IStreamingRequestTransportClient? transportClient;
    private readonly Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>? syncTransportClientFactory;
    private readonly Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>>? asyncTransportClientFactory;

    public StreamingRequestTransportClientFactory(IStreamingRequestTransportClient transportClient)
    {
        this.transportClient = transportClient;
    }

    public StreamingRequestTransportClientFactory(Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>? syncTransportClientFactory)
    {
        this.syncTransportClientFactory = syncTransportClientFactory;
    }

    public StreamingRequestTransportClientFactory(Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>>? asyncTransportClientFactory)
    {
        this.asyncTransportClientFactory = asyncTransportClientFactory;
    }

    public Task<IStreamingRequestTransportClient> Create(Type requestType, IServiceProvider serviceProvider)
    {
        if (transportClient is not null)
        {
            return Task.FromResult(transportClient);
        }

        var transportBuilder = new StreamingRequestTransportClientBuilder(serviceProvider, requestType);

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
