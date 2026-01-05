namespace Conqueror;

public interface IHttpWebSocketsIteratorHandler
{
    static abstract void ConfigureHttpWebSocketsServer(IHttpWebSocketsIteratorServer server);
}

public interface IHttpWebSocketsIteratorHandler<TIterator, TItem, TIHandler>
    : IIteratorHandler<TIterator, TItem, TIHandler>
    where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
    where TIHandler : class, IHttpWebSocketsIteratorHandler<TIterator, TItem, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static IIteratorHandlerTypesInjector CreateHttpWebSocketsTypesInjector<THandler>()
        where THandler : class, TIHandler, IHttpWebSocketsIteratorHandler =>
        new HttpWebSocketsIteratorHandlerTypesInjector<TIterator, TItem, TIHandler>(
            THandler.ConfigureHttpWebSocketsServer
        );
}
