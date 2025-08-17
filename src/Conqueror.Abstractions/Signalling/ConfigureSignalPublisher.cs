namespace Conqueror;

public delegate ISignalPublisher<TSignal> ConfigureSignalPublisher<TSignal>(SignalPublisherBuilder<TSignal> builder)
    where TSignal : class, ISignal<TSignal>;

public delegate ValueTask<ISignalPublisher<TSignal>> ConfigureSignalPublisherAsync<TSignal>(
    SignalPublisherBuilder<TSignal> builder
)
    where TSignal : class, ISignal<TSignal>;
