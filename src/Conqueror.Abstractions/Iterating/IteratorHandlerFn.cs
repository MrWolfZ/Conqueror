namespace Conqueror;

public delegate IAsyncEnumerable<TItem> IteratorHandlerFn<in TIterator, out TItem>(
    TIterator iterator,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
)
    where TIterator : class, IIterator<TIterator, TItem>;
