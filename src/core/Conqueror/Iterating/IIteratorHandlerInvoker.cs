namespace Conqueror.Iterating;

internal interface IIteratorHandlerInvoker
{
    IAsyncEnumerable<TItem> Invoke<TIterator, TItem>(
        TIterator iterator,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TIterator : class, IIterator<TIterator, TItem>;
}
