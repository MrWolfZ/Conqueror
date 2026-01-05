namespace Conqueror.Iterating;

internal sealed class InProcessIteratorClient<TIterator, TItem>(IIteratorServerHandlerInvoker invoker)
    : IIteratorClient<TIterator, TItem>
    where TIterator : class, IIterator<TIterator, TItem>
{
    public string TransportTypeName => ConquerorConstants.InProcessTransportName;

    public IAsyncEnumerable<TItem> Execute(
        TIterator iterator,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    ) => invoker.Invoke<TIterator, TItem>(iterator, serviceProvider, TransportTypeName, cancellationToken);
}
