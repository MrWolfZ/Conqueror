namespace Conqueror.Signalling;

internal sealed class AggregateSignalPublisher<TSignal>(IReadOnlyCollection<ISignalPublisher<TSignal>> publishers)
    : IAggregateSignalPublisher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public string TransportTypeName { get; } =
        $"{ConquerorConstants.AggregateTransportName}[{string.Join(',', publishers.Select(p => p.TransportTypeName))}]";

    private ISignalBroadcastingStrategy BroadcastingStrategy { get; set; } =
        SequentialSignalBroadcastingStrategy.Default;

    public Task Publish(
        TSignal signal,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
    {
        var fns = publishers
            .Select<ISignalPublisher<TSignal>, SignalHandlerFn<TSignal>>(p =>
                (s, sp, ct) => p.Publish(s, sp, conquerorContext, ct)
            )
            .ToList();

        return BroadcastingStrategy.BroadcastSignal(fns, serviceProvider, signal, cancellationToken);
    }

    public IAggregateSignalPublisher<TSignal> WithBroadcastingStrategy(ISignalBroadcastingStrategy broadcastingStrategy)
    {
        BroadcastingStrategy = broadcastingStrategy;

        return this;
    }

    public IAggregateSignalPublisher<TSignal> WithSequentialBroadcastingStrategy() =>
        WithBroadcastingStrategy(SequentialSignalBroadcastingStrategy.Default);

    public IAggregateSignalPublisher<TSignal> WithSequentialBroadcastingStrategy(
        Action<SequentialSignalBroadcastingStrategyConfiguration> configure
    )
    {
        var configuration = new SequentialSignalBroadcastingStrategyConfiguration();
        configure(configuration);

        return WithBroadcastingStrategy(new SequentialSignalBroadcastingStrategy(configuration));
    }

    public IAggregateSignalPublisher<TSignal> WithParallelBroadcastingStrategy() =>
        WithBroadcastingStrategy(ParallelSignalBroadcastingStrategy.Default);

    public IAggregateSignalPublisher<TSignal> WithParallelBroadcastingStrategy(
        Action<ParallelSignalBroadcastingStrategyConfiguration> configure
    )
    {
        var configuration = new ParallelSignalBroadcastingStrategyConfiguration();
        configure(configuration);

        return WithBroadcastingStrategy(new ParallelSignalBroadcastingStrategy(configuration));
    }
}
