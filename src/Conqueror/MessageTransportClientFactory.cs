using System;
using System.Threading.Tasks;

namespace Conqueror;

internal sealed class MessageTransportClientFactory<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    private readonly IMessageTransportClient? transportClient;
    private readonly ConfigureMessageTransportClient<TMessage, TResponse>? syncTransportClientFactory;
    private readonly ConfigureMessageTransportClientAsync<TMessage, TResponse>? asyncTransportClientFactory;

    public MessageTransportClientFactory(IMessageTransportClient transportClient)
    {
        this.transportClient = transportClient;
    }

    public MessageTransportClientFactory(ConfigureMessageTransportClient<TMessage, TResponse>? syncTransportClientFactory)
    {
        this.syncTransportClientFactory = syncTransportClientFactory;
    }

    public MessageTransportClientFactory(ConfigureMessageTransportClientAsync<TMessage, TResponse>? asyncTransportClientFactory)
    {
        this.asyncTransportClientFactory = asyncTransportClientFactory;
    }

    public Task<IMessageTransportClient> Create(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (transportClient is not null)
        {
            return Task.FromResult(transportClient);
        }

        var transportBuilder = new MessageTransportClientBuilder<TMessage, TResponse>(serviceProvider, conquerorContext);

        if (syncTransportClientFactory is not null)
        {
            return Task.FromResult(syncTransportClientFactory.Invoke(transportBuilder));
        }

        if (asyncTransportClientFactory is not null)
        {
            return asyncTransportClientFactory.Invoke(transportBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create transport client for message type '{typeof(TMessage)}' since it was not configured with a factory");
    }
}
