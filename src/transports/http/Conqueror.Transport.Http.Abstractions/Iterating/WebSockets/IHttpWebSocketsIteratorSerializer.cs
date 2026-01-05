namespace Conqueror;

internal interface IHttpWebSocketsIteratorSerializer<TIterator>
{
    Task SerializeIterator(
        IServiceProvider serviceProvider,
        TIterator iterator,
        Stream stream,
        CancellationToken cancellationToken
    );

    Task<TIterator> DeserializeIterator(
        IServiceProvider serviceProvider,
        Stream stream,
        CancellationToken cancellationToken
    );
}

internal interface IHttpWebSocketsItemSerializer<TItem>
{
    Task SerializeItem(
        IServiceProvider serviceProvider,
        TItem item,
        Stream stream,
        CancellationToken cancellationToken
    );

    Task<TItem> DeserializeItem(IServiceProvider serviceProvider, Stream stream, CancellationToken cancellationToken);
}
