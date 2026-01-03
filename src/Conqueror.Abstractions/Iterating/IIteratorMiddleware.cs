namespace Conqueror;

public interface IIteratorMiddleware<TIterator, TItem>
    where TIterator : class, IIterator<TIterator, TItem>
{
    IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx);
}
