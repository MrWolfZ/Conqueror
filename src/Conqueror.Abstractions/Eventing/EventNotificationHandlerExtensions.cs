using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class EventNotificationHandlerExtensions
{
    public static THandler WithPipeline<TEventNotification, THandler>(this IEventNotificationHandler<TEventNotification, THandler> handler,
                                                                      Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
    {
        if (handler is IConfigurableEventNotificationHandler<TEventNotification, THandler> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static THandler WithPublisher<TEventNotification, THandler>(this IEventNotificationHandler<TEventNotification, THandler> handler,
                                                                       ConfigureEventNotificationPublisher<TEventNotification> configurePublisher)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
    {
        if (handler is IConfigurableEventNotificationHandler<TEventNotification, THandler> c)
        {
            return c.WithPublisher(configurePublisher);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPublisher)}", nameof(handler), null);
    }

    public static THandler WithPublisher<TEventNotification, THandler>(this IEventNotificationHandler<TEventNotification, THandler> handler,
                                                                       ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
    {
        if (handler is IConfigurableEventNotificationHandler<TEventNotification, THandler> c)
        {
            return c.WithPublisher(configurePublisher);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPublisher)}", nameof(handler), null);
    }
}
