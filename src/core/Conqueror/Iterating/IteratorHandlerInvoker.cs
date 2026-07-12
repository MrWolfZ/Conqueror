namespace Conqueror.Iterating;

internal sealed class IteratorHandlerInvoker<TIterator, TItem> : IIteratorHandlerInvoker
    where TIterator : class, IIterator<TIterator, TItem>
{
    private readonly IteratorDispatcher dispatcher;
    private readonly IteratorHandlerFn<TIterator, TItem> handlerFn;
    private readonly IteratorPipeline<TIterator, TItem> pipeline;

    public IteratorHandlerInvoker(
        IServiceProvider serviceProvider,
        IConquerorContextAccessor conquerorContextAccessor,
        IIteratorIdFactory iteratorIdFactory,
        Action<IIteratorPipeline<TIterator, TItem>>? configurePipeline,
        IteratorHandlerFn<TIterator, TItem> handlerFn,
        Type? handlerType
    )
    {
        this.handlerFn = handlerFn;
        dispatcher = new IteratorDispatcher(conquerorContextAccessor, iteratorIdFactory, IteratorTransportRole.Server);
        pipeline = new IteratorPipeline<TIterator, TItem>(handlerType, serviceProvider);

        configurePipeline?.Invoke(pipeline);
    }

    public IAsyncEnumerable<TItm> Invoke<TItr, TItm>(
        TItr iterator,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TItr : class, IIterator<TItr, TItm>
    {
        Debug.Assert(
            typeof(TItr) == typeof(TIterator),
            $"the iterator type was expected to be {typeof(TIterator)}, but was {typeof(TItr)} instead."
        );
        Debug.Assert(
            typeof(TItm) == typeof(TItem),
            $"the item type was expected to be {typeof(TItem)}, but was {typeof(TItm)} instead."
        );

        return (IAsyncEnumerable<TItm>)
            (object)
            dispatcher.Dispatch(
                (iterator as TIterator)!,
                serviceProvider,
                pipeline,
                new Client(handlerFn, transportTypeName),
                configureClientAsync: null,
                cancellationToken
            );
    }

    private sealed class Client(IteratorHandlerFn<TIterator, TItem> handlerFn, string transportTypeName)
        : IIteratorClient<TIterator, TItem>
    {
        public string TransportTypeName { get; } = transportTypeName;

        public IAsyncEnumerable<TItem> Execute(
            TIterator iterator,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        ) => handlerFn(iterator, serviceProvider, cancellationToken);
    }
}
