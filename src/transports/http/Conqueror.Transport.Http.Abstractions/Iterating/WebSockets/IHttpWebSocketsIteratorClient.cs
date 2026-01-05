namespace Conqueror;

public delegate Task<WebSocket> HttpWebSocketsIteratorWebSocketFactory(
    Uri address,
    CancellationToken cancellationToken
);

public interface IHttpWebSocketsIteratorClient<in TIterator, out TItem> : IIteratorClient<TIterator, TItem>
    where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
{
    /// <summary>
    ///     Use the provided factory to create a <see cref="WebSocket" /> which is connected to the target endpoint.
    /// </summary>
    /// <param name="webSocketFactory">The factory to create a connected web socket</param>
    /// <returns>The transport client configured to use the web socket factory</returns>
    IHttpWebSocketsIteratorClient<TIterator, TItem> WithWebSocketFactory(
        HttpWebSocketsIteratorWebSocketFactory webSocketFactory
    );

    /// <summary>
    ///     Configure the number of items to prefetch from the server. The client will request this many items
    ///     from the server in advance and buffer them locally. When the buffer runs low, the client will
    ///     automatically request more items. Higher values reduce network round-trips but increase memory usage.
    ///     Defaults to 1 (no prefetching).
    /// </summary>
    /// <param name="prefetchCount">The number of items to prefetch (must be at least 1)</param>
    /// <returns>The transport client configured with the specified prefetch count</returns>
    IHttpWebSocketsIteratorClient<TIterator, TItem> WithPrefetchCount(int prefetchCount);

    /// <summary>
    ///     Configure the heartbeat interval for keeping the WebSocket connection alive. The client will send
    ///     ping messages at this interval to detect connection issues. Defaults to 10 seconds.
    /// </summary>
    /// <param name="heartbeatInterval">The heartbeat interval (must be a multiple of 1 second)</param>
    /// <returns>The transport client configured with the specified heartbeat interval</returns>
    IHttpWebSocketsIteratorClient<TIterator, TItem> WithHeartbeatInterval(TimeSpan heartbeatInterval);

    /// <summary>
    ///     Configure the heartbeat timeout for detecting connection failures. If no response is received
    ///     within this timeout, the connection is considered failed. Defaults to 30 seconds.
    /// </summary>
    /// <param name="heartbeatTimeout">The heartbeat timeout (must be a multiple of 1 second and greater than the heartbeat interval)</param>
    /// <returns>The transport client configured with the specified heartbeat timeout</returns>
    IHttpWebSocketsIteratorClient<TIterator, TItem> WithHeartbeatTimeout(TimeSpan heartbeatTimeout);
}
