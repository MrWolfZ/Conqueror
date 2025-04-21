using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

    public static TGeneratedHandlerInterface AsIHandler<TEventNotification, TGeneratedHandlerInterface>(this IEventNotificationHandler<TEventNotification> handler)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where TGeneratedHandlerInterface : class, IEventNotificationHandler<TEventNotification>, IGeneratedEventNotificationHandler
    {
        return (TGeneratedHandlerInterface)TEventNotification.DefaultTypeInjector.CreateWithEventNotificationTypes(new HandlerCastInjectable<TEventNotification>(handler));
    }

    private sealed class HandlerCastInjectable<TEventNotificationParam>(
        IEventNotificationHandler<TEventNotificationParam> handler)
        : IDefaultEventNotificationTypesInjectable<IEventNotificationHandler<TEventNotificationParam>>
        where TEventNotificationParam : class, IEventNotification<TEventNotificationParam>
    {
        IEventNotificationHandler<TEventNotificationParam> IDefaultEventNotificationTypesInjectable<IEventNotificationHandler<TEventNotificationParam>>
            .WithInjectedTypes<
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                TEventNotification,
                TGeneratedHandlerInterface,
                TGeneratedHandlerAdapter>()
        {
            Debug.Assert(typeof(TEventNotification) == typeof(TEventNotificationParam), $"expected event notification type '{typeof(TEventNotification)}', but got '{typeof(TEventNotificationParam)}')");

            var adapter = new TGeneratedHandlerAdapter
            {
                Wrapped = (IEventNotificationHandler<TEventNotification>)handler,
            };

            return adapter as IEventNotificationHandler<TEventNotificationParam> ?? throw new InvalidOperationException("could not create handler adapter");
        }
    }
}
