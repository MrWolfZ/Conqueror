namespace Conqueror;

public interface ISignalBroadcastingStrategy
{
    Task BroadcastSignal<TSignal>(
        IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
        IServiceProvider serviceProvider,
        TSignal signal,
        CancellationToken cancellationToken
    )
        where TSignal : class, ISignal<TSignal>;
}
