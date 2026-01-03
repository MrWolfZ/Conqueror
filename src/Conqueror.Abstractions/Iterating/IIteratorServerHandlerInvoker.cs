namespace Conqueror;

public interface IIteratorServerHandlerInvoker
{
    Type IteratorType { get; }

    Type ItemType { get; }

    Type? HandlerType { get; }

    IAsyncEnumerable<TItem> Invoke<TIterator, TItem>(
        TIterator iterator,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TIterator : class, IIterator<TIterator, TItem>;
}

public interface IIteratorServerHandlerInvoker<out TTypesInjector> : IIteratorServerHandlerInvoker
    where TTypesInjector : class, IIteratorHandlerTypesInjector
{
    TTypesInjector TypesInjector { get; }
}
