using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class EventNotificationHandlerExtensions
{
    public static IEventNotificationHandler<TEventNotification> WithPipeline<TEventNotification>(this IEventNotificationHandler<TEventNotification> handler,
                                                                                                 Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        if (handler is IConfigurableEventNotificationHandler<TEventNotification> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static IEventNotificationHandler<TEventNotification> WithPublisher<TEventNotification>(this IEventNotificationHandler<TEventNotification> handler,
                                                                                                  ConfigureEventNotificationPublisher<TEventNotification> configurePublisher)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        if (handler is IConfigurableEventNotificationHandler<TEventNotification> c)
        {
            return c.WithPublisher(configurePublisher);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPublisher)}", nameof(handler), null);
    }

    public static IEventNotificationHandler<TEventNotification> WithPublisher<TEventNotification>(this IEventNotificationHandler<TEventNotification> handler,
                                                                                                  ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        if (handler is IConfigurableEventNotificationHandler<TEventNotification> c)
        {
            return c.WithPublisher(configurePublisher);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPublisher)}", nameof(handler), null);
    }

    public static THandler AsIHandler<TEventNotification, THandler>(this IEventNotificationHandler<TEventNotification> handler)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification>, IGeneratedEventNotificationHandler
    {
        return TEventNotification.DefaultTypeInjector.CreateWithEventNotificationTypes(new HandlerCastInjectable<THandler>(handler));
    }

    private sealed class HandlerCastInjectable<THandler>(object handler) : IDefaultEventNotificationTypesInjectable<THandler>
        where THandler : class
    {
        public THandler WithInjectedTypes<TEventNotification, TGeneratedHandlerInterface, TGeneratedHandlerAdapter>()
            where TEventNotification : class, IEventNotification<TEventNotification>
            where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification>
            where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification>, TGeneratedHandlerInterface, new()
        {
            Debug.Assert(typeof(TGeneratedHandlerInterface) == typeof(THandler), "result handler type should always be equal to generated handler interface");
            Debug.Assert(handler is IEventNotificationHandler<TEventNotification>, "handler to wrap must be of correct type");

            var adapter = new TGeneratedHandlerAdapter
            {
                Wrapped = (IEventNotificationHandler<TEventNotification>)handler,
            };

            return adapter as THandler ?? throw new InvalidOperationException("could not create handler adapter");
        }
    }
}
