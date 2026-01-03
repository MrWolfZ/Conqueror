namespace Conqueror;

public delegate IIteratorClient<TIterator, TItem> ConfigureIteratorClient<TIterator, TItem>(
    IteratorClientBuilder<TIterator, TItem> builder
)
    where TIterator : class, IIterator<TIterator, TItem>;

public delegate ValueTask<IIteratorClient<TIterator, TItem>> ConfigureIteratorClientAsync<TIterator, TItem>(
    IteratorClientBuilder<TIterator, TItem> builder
)
    where TIterator : class, IIterator<TIterator, TItem>;
