namespace Conqueror;

public readonly record struct SignalMiddlewareContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    private readonly State state;

    public SignalMiddlewareContext(
        List<ISignalMiddleware<TSignal>> middlewares,
        ISignalPublisher<TSignal> publisher,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        SignalTransportType transportType
    )
    {
        Debug.Assert(middlewares.Count > 0, "this should only be called if there are middlewares to execute");

        state = new State
        {
            Middlewares = middlewares,
            Publisher = publisher,
            ServiceProvider = serviceProvider,
            ConquerorContext = conquerorContext,
            TransportType = transportType,
        };
    }

    public required TSignal Signal { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public IServiceProvider ServiceProvider => state.ServiceProvider;

    public ConquerorContext ConquerorContext => state.ConquerorContext;

    public SignalTransportType TransportType => state.TransportType;

    private int CurrentIndex { get; init; }

    public Task Next(TSignal signal, CancellationToken cancellationToken)
    {
        var nextIndex = CurrentIndex + 1;
        if (nextIndex < state.Middlewares.Count)
        {
            var updatedContext = this with
            {
                Signal = signal,
                CancellationToken = cancellationToken,
                CurrentIndex = nextIndex,
            };

            return state.Middlewares[nextIndex].Execute(updatedContext);
        }

        return state.Publisher.Publish(signal, ServiceProvider, ConquerorContext, cancellationToken);
    }

    // performance optimization: we capture the immutable parts of the context in a separate
    // record so that it only needs to be allocated once per pipeline execution instead of
    // being embedded in the context, which would require a lot of copying of fields, especially
    // when the context might be captured in the async state machine of a middleware's Execute
    // method
    private sealed record State
    {
        public required List<ISignalMiddleware<TSignal>> Middlewares { get; init; }

        public required ISignalPublisher<TSignal> Publisher { get; init; }

        public required IServiceProvider ServiceProvider { get; init; }

        public required ConquerorContext ConquerorContext { get; init; }

        public required SignalTransportType TransportType { get; init; }
    }
}
