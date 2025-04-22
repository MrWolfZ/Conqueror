using System;
using Conqueror.Signalling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessSignalPublisherBuilderExtensions
{
    public static ISignalPublisher<TSignal> UseInProcessWithSequentialBroadcastingStrategy<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, ISignal<TSignal>
        => builder.UseInProcess(SequentialSignalBroadcastingStrategy.Default);

    public static ISignalPublisher<TSignal> UseInProcessWithSequentialBroadcastingStrategy<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder,
        Action<SequentialSignalBroadcastingStrategyConfiguration> configure)
        where TSignal : class, ISignal<TSignal>
    {
        var configuration = new SequentialSignalBroadcastingStrategyConfiguration();
        configure(configuration);
        return builder.UseInProcess(new SequentialSignalBroadcastingStrategy(configuration));
    }

    public static ISignalPublisher<TSignal> UseInProcessWithParallelBroadcastingStrategy<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, ISignal<TSignal>
        => builder.UseInProcess(ParallelSignalBroadcastingStrategy.Default);

    public static ISignalPublisher<TSignal> UseInProcessWithParallelBroadcastingStrategy<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder,
        Action<ParallelSignalBroadcastingStrategyConfiguration> configure)
        where TSignal : class, ISignal<TSignal>
    {
        var configuration = new ParallelSignalBroadcastingStrategyConfiguration();
        configure(configuration);
        return builder.UseInProcess(new ParallelSignalBroadcastingStrategy(configuration));
    }

    // TODO: implement (and test)
    public static ISignalPublisher<TSignal> UseInProcessWithFireAndForgetBroadcastingStrategy<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, ISignal<TSignal>
    {
        throw new NotImplementedException();
    }

    public static ISignalPublisher<TSignal> UseInProcess<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder,
        ISignalBroadcastingStrategy broadcastingStrategy)
        where TSignal : class, ISignal<TSignal>
    {
        return new InProcessSignalPublisher<TSignal>(broadcastingStrategy);
    }
}
