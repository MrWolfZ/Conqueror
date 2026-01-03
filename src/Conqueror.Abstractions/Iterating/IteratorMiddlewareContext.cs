namespace Conqueror;

public readonly record struct IteratorMiddlewareContext<TIterator, TItem>
    where TIterator : class, IIterator<TIterator, TItem>
{
    private readonly State state;

    public IteratorMiddlewareContext(
        List<IIteratorMiddleware<TIterator, TItem>> middlewares,
        IIteratorClient<TIterator, TItem> client,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        IteratorTransportType transportType
    )
    {
        Debug.Assert(middlewares.Count > 0, "this should only be called if there are middlewares to execute");

        state = new State
        {
            Middlewares = middlewares,
            Client = client,
            ServiceProvider = serviceProvider,
            ConquerorContext = conquerorContext,
            TransportType = transportType,
        };
    }

    public required TIterator Iterator { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public IServiceProvider ServiceProvider => state.ServiceProvider;

    public ConquerorContext ConquerorContext => state.ConquerorContext;

    public IteratorTransportType TransportType => state.TransportType;

    private int CurrentIndex { get; init; }

    public IAsyncEnumerable<TItem> Next(TIterator iterator, CancellationToken cancellationToken)
    {
        var nextIndex = CurrentIndex + 1;
        if (nextIndex < state.Middlewares.Count)
        {
            var updatedContext = this with
            {
                Iterator = iterator,
                CancellationToken = cancellationToken,
                CurrentIndex = nextIndex,
            };

            return state.Middlewares[nextIndex].Execute(updatedContext);
        }

        return state.Client.Execute(iterator, ServiceProvider, ConquerorContext, cancellationToken);
    }

    // performance optimization: we capture the immutable parts of the context in a separate
    // record so that it only needs to be allocated once per pipeline execution instead of
    // being embedded in the context, which would require a lot of copying of fields, especially
    // when the context might be captured in the async state machine of a middleware's Execute
    // method
    private sealed record State
    {
        public required List<IIteratorMiddleware<TIterator, TItem>> Middlewares { get; init; }

        public required IIteratorClient<TIterator, TItem> Client { get; init; }

        public required IServiceProvider ServiceProvider { get; init; }

        public required ConquerorContext ConquerorContext { get; init; }

        public required IteratorTransportType TransportType { get; init; }
    }
}
