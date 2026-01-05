namespace Conqueror;

public interface IHttpWebSocketsIteratorClientFactory
{
    IHttpWebSocketsIteratorClient<TIterator, TItem> Get<TIterator, TItem>()
        where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>;
}
