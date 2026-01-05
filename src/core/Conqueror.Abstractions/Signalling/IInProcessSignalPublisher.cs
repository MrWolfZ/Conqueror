namespace Conqueror;

using Signalling;

public interface IInProcessSignalPublisher<in TSignal> : ISignalPublisher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    IInProcessSignalPublisher<TSignal> WithBroadcastingStrategy(ISignalBroadcastingStrategy broadcastingStrategy);

    IInProcessSignalPublisher<TSignal> WithSequentialBroadcastingStrategy();

    IInProcessSignalPublisher<TSignal> WithSequentialBroadcastingStrategy(
        Action<SequentialSignalBroadcastingStrategyConfiguration> configure
    );

    IInProcessSignalPublisher<TSignal> WithParallelBroadcastingStrategy();

    IInProcessSignalPublisher<TSignal> WithParallelBroadcastingStrategy(
        Action<ParallelSignalBroadcastingStrategyConfiguration> configure
    );

    // TODO: implement (and test)
    // IInProcessSignalPublisher<TSignal> WithFireAndForgetBroadcastingStrategy();
    //
    // IInProcessSignalPublisher<TSignal> WithFireAndForgetBroadcastingStrategy(Action<FireAndForgetSignalBroadcastingStrategyConfiguration> configure);
}
