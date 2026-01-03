namespace Conqueror;

public interface IIteratorClient<in TIterator, out TItem>
    where TIterator : class, IIterator<TIterator, TItem>
{
    string TransportTypeName { get; }

    IAsyncEnumerable<TItem> Execute(
        TIterator iterator,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    );
}
