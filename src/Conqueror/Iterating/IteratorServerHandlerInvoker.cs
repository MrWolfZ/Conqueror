namespace Conqueror.Iterating;

internal sealed class IteratorServerHandlerInvoker<TTypesInjector>(
    IteratorHandlerRegistration registration,
    IIteratorHandlerInvoker handlerInvoker,
    TTypesInjector typesInjector
) : IIteratorServerHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, IIteratorHandlerTypesInjector
{
    public Type IteratorType { get; } = registration.IteratorType;

    public Type ItemType { get; } = registration.ItemType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public IAsyncEnumerable<TItem> Invoke<TIterator, TItem>(
        TIterator iterator,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TIterator : class, IIterator<TIterator, TItem>
    {
        return handlerInvoker.Invoke<TIterator, TItem>(iterator, serviceProvider, transportTypeName, cancellationToken);
    }
}
