using System;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPublisherFactory<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    private readonly IEventNotificationPublisher<TEventNotification>? transportClient;
    private readonly ConfigureEventNotificationPublisher<TEventNotification>? syncTransportClientFactory;
    private readonly ConfigureEventNotificationPublisherAsync<TEventNotification>? asyncTransportClientFactory;

    public EventNotificationPublisherFactory(IEventNotificationPublisher<TEventNotification> transportClient)
    {
        this.transportClient = transportClient;
    }

    public EventNotificationPublisherFactory(ConfigureEventNotificationPublisher<TEventNotification>? syncTransportClientFactory)
    {
        this.syncTransportClientFactory = syncTransportClientFactory;
    }

    public EventNotificationPublisherFactory(ConfigureEventNotificationPublisherAsync<TEventNotification>? asyncTransportClientFactory)
    {
        this.asyncTransportClientFactory = asyncTransportClientFactory;
    }

    public Task<IEventNotificationPublisher<TEventNotification>> Create(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (transportClient is not null)
        {
            return Task.FromResult(transportClient);
        }

        var publisherBuilder = new EventNotificationPublisherBuilder<TEventNotification>(serviceProvider, conquerorContext);

        if (syncTransportClientFactory is not null)
        {
            return Task.FromResult(syncTransportClientFactory.Invoke(publisherBuilder));
        }

        if (asyncTransportClientFactory is not null)
        {
            return asyncTransportClientFactory.Invoke(publisherBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create publisher for event notification type '{typeof(TEventNotification)}' since it was not configured with a factory");
    }
}
