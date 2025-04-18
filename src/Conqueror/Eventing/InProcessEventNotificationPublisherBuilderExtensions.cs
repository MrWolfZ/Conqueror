using System;
using Conqueror.Eventing;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessEventNotificationPublisherBuilderExtensions
{
    public static IEventNotificationPublisher<TEventNotification> UseInProcessWithSequentialBroadcastingStrategy<TEventNotification>(
        this IEventNotificationPublisherBuilder<TEventNotification> builder)
        where TEventNotification : class, IEventNotification<TEventNotification>
        => builder.UseInProcess(SequentialEventNotificationBroadcastingStrategy.Default);

    public static IEventNotificationPublisher<TEventNotification> UseInProcessWithSequentialBroadcastingStrategy<TEventNotification>(
        this IEventNotificationPublisherBuilder<TEventNotification> builder,
        Action<SequentialEventNotificationBroadcastingStrategyConfiguration> configure)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var configuration = new SequentialEventNotificationBroadcastingStrategyConfiguration();
        configure(configuration);
        return builder.UseInProcess(new SequentialEventNotificationBroadcastingStrategy(configuration));
    }

    public static IEventNotificationPublisher<TEventNotification> UseInProcessWithParallelBroadcastingStrategy<TEventNotification>(
        this IEventNotificationPublisherBuilder<TEventNotification> builder)
        where TEventNotification : class, IEventNotification<TEventNotification>
        => builder.UseInProcess(ParallelEventNotificationBroadcastingStrategy.Default);

    public static IEventNotificationPublisher<TEventNotification> UseInProcessWithParallelBroadcastingStrategy<TEventNotification>(
        this IEventNotificationPublisherBuilder<TEventNotification> builder,
        Action<ParallelEventNotificationBroadcastingStrategyConfiguration> configure)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var configuration = new ParallelEventNotificationBroadcastingStrategyConfiguration();
        configure(configuration);
        return builder.UseInProcess(new ParallelEventNotificationBroadcastingStrategy(configuration));
    }

    // TODO: implement (and test)
    public static IEventNotificationPublisher<TEventNotification> UseInProcessWithFireAndForgetBroadcastingStrategy<TEventNotification>(
        this IEventNotificationPublisherBuilder<TEventNotification> builder)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        throw new NotImplementedException();
    }

    public static IEventNotificationPublisher<TEventNotification> UseInProcess<TEventNotification>(
        this IEventNotificationPublisherBuilder<TEventNotification> builder,
        IEventNotificationBroadcastingStrategy broadcastingStrategy)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return new InProcessEventNotificationPublisher<TEventNotification>(broadcastingStrategy);
    }
}
