namespace Conqueror;

public interface IIteratorHandlerRegistry
{
    IIteratorServerHandlerInvoker<TTypesInjector>? GetServerHandlerInvoker<TIterator, TItem, TTypesInjector>()
        where TIterator : class, IIterator<TIterator, TItem>
        where TTypesInjector : class, IIteratorHandlerTypesInjector;

    IReadOnlyCollection<IIteratorServerHandlerInvoker<TTypesInjector>> GetServerHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, IIteratorHandlerTypesInjector;
}
