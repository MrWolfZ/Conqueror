namespace Conqueror;

public interface IInProcessIteratorClientFactory
{
    IIteratorClient<TIterator, TItem> Get<TIterator, TItem>()
        where TIterator : class, IIterator<TIterator, TItem>;

    IIteratorClient<TIterator, TItem>? GetIfAvailable<TIterator, TItem>()
        where TIterator : class, IIterator<TIterator, TItem>;
}
