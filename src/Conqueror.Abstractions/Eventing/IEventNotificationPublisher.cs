using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate IEventNotificationPublisher<TEventNotification> ConfigureEventNotificationPublisher<TEventNotification>(
    IEventNotificationPublisherBuilder<TEventNotification> builder)
    where TEventNotification : class, IEventNotification<TEventNotification>;

public delegate Task<IEventNotificationPublisher<TEventNotification>> ConfigureEventNotificationPublisherAsync<TEventNotification>(
    IEventNotificationPublisherBuilder<TEventNotification> builder)
    where TEventNotification : class, IEventNotification<TEventNotification>;

public interface IEventNotificationPublisher<in TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    string TransportTypeName { get; }

    Task Publish(TEventNotification notification,
                 IServiceProvider serviceProvider,
                 ConquerorContext conquerorContext,
                 CancellationToken cancellationToken);
}

public interface IEventNotificationPublisherBuilder<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }
}
