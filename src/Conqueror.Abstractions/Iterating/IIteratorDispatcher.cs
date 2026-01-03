namespace Conqueror;

internal interface IIteratorDispatcher
{
    IAsyncEnumerable<TItem> Dispatch<TIterator, TItem>(
        TIterator iterator,
        IServiceProvider serviceProvider,
        IIteratorPipeline<TIterator, TItem> pipeline,
        IIteratorClient<TIterator, TItem>? client,
        ConfigureIteratorClientAsync<TIterator, TItem>? configureClientAsync,
        CancellationToken cancellationToken
    )
        where TIterator : class, IIterator<TIterator, TItem>;
}
