using System;
using Conqueror.Signalling;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IAggregateSignalPublisher<in TSignal> : ISignalPublisher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    IAggregateSignalPublisher<TSignal> WithBroadcastingStrategy(ISignalBroadcastingStrategy broadcastingStrategy);

    IAggregateSignalPublisher<TSignal> WithSequentialBroadcastingStrategy();

    IAggregateSignalPublisher<TSignal> WithSequentialBroadcastingStrategy(Action<SequentialSignalBroadcastingStrategyConfiguration> configure);

    IAggregateSignalPublisher<TSignal> WithParallelBroadcastingStrategy();

    IAggregateSignalPublisher<TSignal> WithParallelBroadcastingStrategy(Action<ParallelSignalBroadcastingStrategyConfiguration> configure);

    // TODO: implement (and test)
    // IAggregateSignalPublisher<TSignal> WithFireAndForgetBroadcastingStrategy();
    //
    // IAggregateSignalPublisher<TSignal> WithFireAndForgetBroadcastingStrategy(Action<FireAndForgetSignalBroadcastingStrategyConfiguration> configure);
}
