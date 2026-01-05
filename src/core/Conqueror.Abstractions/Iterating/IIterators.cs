namespace Conqueror;

public interface IIterators
{
    TIHandler For<TIterator, TItem, TIHandler>(IteratorTypes<TIterator, TItem, TIHandler> iteratorTypes)
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>;
}
