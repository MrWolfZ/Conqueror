namespace Conqueror;

public delegate Task SignalHandlerFn<in TSignal>(
    TSignal signal,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
)
    where TSignal : class, ISignal<TSignal>;

public delegate void SignalHandlerSyncFn<in TSignal>(TSignal signal, IServiceProvider serviceProvider)
    where TSignal : class, ISignal<TSignal>;
