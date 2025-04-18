using System;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPublisherFactory<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    private readonly IEventNotificationPublisher<TEventNotification>? publisher;
    private readonly ConfigureEventNotificationPublisher<TEventNotification>? syncPublisherFactory;
    private readonly ConfigureEventNotificationPublisherAsync<TEventNotification>? asyncPublisherFactory;

    public EventNotificationPublisherFactory(IEventNotificationPublisher<TEventNotification> publisher)
    {
        this.publisher = publisher;
    }

    public EventNotificationPublisherFactory(ConfigureEventNotificationPublisher<TEventNotification>? syncPublisherFactory)
    {
        this.syncPublisherFactory = syncPublisherFactory;
    }

    public EventNotificationPublisherFactory(ConfigureEventNotificationPublisherAsync<TEventNotification>? asyncPublisherFactory)
    {
        this.asyncPublisherFactory = asyncPublisherFactory;
    }

    public Task<IEventNotificationPublisher<TEventNotification>> Create(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (publisher is not null)
        {
            return Task.FromResult(publisher);
        }

        var publisherBuilder = new EventNotificationPublisherBuilder<TEventNotification>(serviceProvider, conquerorContext);

        if (syncPublisherFactory is not null)
        {
            return Task.FromResult(syncPublisherFactory.Invoke(publisherBuilder));
        }

        if (asyncPublisherFactory is not null)
        {
            return asyncPublisherFactory.Invoke(publisherBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create publisher for event notification type '{typeof(TEventNotification)}' since it was not configured with a factory");
    }
}
