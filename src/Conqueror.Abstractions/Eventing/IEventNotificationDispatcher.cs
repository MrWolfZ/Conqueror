using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal interface IEventNotificationDispatcher<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    Task Dispatch(TEventNotification notification, CancellationToken cancellationToken);

    IEventNotificationDispatcher<TEventNotification> WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline);

    IEventNotificationDispatcher<TEventNotification> WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher);

    IEventNotificationDispatcher<TEventNotification> WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher);
}
