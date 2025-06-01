using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Signalling;

internal sealed class InProcessSignalPublisher<TSignal> : IInProcessSignalPublisher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public string TransportTypeName => ConquerorConstants.InProcessTransportName;

    private ISignalBroadcastingStrategy BroadcastingStrategy { get; set; } = SequentialSignalBroadcastingStrategy.Default;

    public Task Publish(TSignal signal,
                        IServiceProvider serviceProvider,
                        ConquerorContext conquerorContext,
                        CancellationToken cancellationToken)
    {
        return serviceProvider.GetRequiredService<InProcessSignalReceiver>()
                              .Broadcast(signal, serviceProvider, BroadcastingStrategy, cancellationToken);
    }

    public IInProcessSignalPublisher<TSignal> WithBroadcastingStrategy(ISignalBroadcastingStrategy broadcastingStrategy)
    {
        BroadcastingStrategy = broadcastingStrategy;
        return this;
    }

    public IInProcessSignalPublisher<TSignal> WithSequentialBroadcastingStrategy()
        => WithBroadcastingStrategy(SequentialSignalBroadcastingStrategy.Default);

    public IInProcessSignalPublisher<TSignal> WithSequentialBroadcastingStrategy(Action<SequentialSignalBroadcastingStrategyConfiguration> configure)
    {
        var configuration = new SequentialSignalBroadcastingStrategyConfiguration();
        configure(configuration);
        return WithBroadcastingStrategy(new SequentialSignalBroadcastingStrategy(configuration));
    }

    public IInProcessSignalPublisher<TSignal> WithParallelBroadcastingStrategy()
        => WithBroadcastingStrategy(ParallelSignalBroadcastingStrategy.Default);

    public IInProcessSignalPublisher<TSignal> WithParallelBroadcastingStrategy(Action<ParallelSignalBroadcastingStrategyConfiguration> configure)
    {
        var configuration = new ParallelSignalBroadcastingStrategyConfiguration();
        configure(configuration);
        return WithBroadcastingStrategy(new ParallelSignalBroadcastingStrategy(configuration));
    }
}
