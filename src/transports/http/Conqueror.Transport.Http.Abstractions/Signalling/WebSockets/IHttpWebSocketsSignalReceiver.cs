using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpWebSocketsSignalReceiver
{
    /// <summary>
    ///     Note that this is (usually) the service provider from the global scope,
    ///     and <i>not</i> the service provider from the scope of the send operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }
    HttpWebSocketsSignalReceiverConfiguration Enable(Uri address);

    void Disable();
}

public delegate Task<WebSocket> HttpWebSocketsSignalWebSocketFactory(
    Uri address,
    CancellationToken cancellationToken);

public sealed class HttpWebSocketsSignalReceiverConfiguration
{
    public HttpWebSocketsSignalReceiverConfiguration()
    {
        WebSocketFactory = DefaultWebSocketFactory;
    }

    public required Uri Address { get; init; }

    public HttpWebSocketsSignalWebSocketFactory WebSocketFactory { get; private set; }

    public TimeSpan HeartbeatInterval { get; private set; } = TimeSpan.FromSeconds(10);

    public TimeSpan HeartbeatTimeout { get; private set; } = TimeSpan.FromSeconds(30);

    public HttpWebSocketsSignalReceiverReconnectDelayFn? ReconnectDelayFn { get; private set; }

    // for now, we'll keep these callback APIs internal for testing and debugging, but we may expose them in the future
    internal Action<object>? SignalCallback { get; private set; }

    internal Action<Exception>? ExceptionCallback { get; private set; }

    /// <summary>
    ///     Use the provided factory to create a <see cref="WebSocket" /> which is connected to the target endpoint.
    /// </summary>
    /// <param name="webSocketFactory">The factory to create a connected web socket</param>
    /// <returns>The updated configuration for the receiver</returns>
    public HttpWebSocketsSignalReceiverConfiguration WithWebSocketFactory(HttpWebSocketsSignalWebSocketFactory webSocketFactory)
    {
        WebSocketFactory = webSocketFactory;

        return this;
    }

    public HttpWebSocketsSignalReceiverConfiguration WithHeartbeatInterval(TimeSpan heartbeatInterval)
    {
        if (heartbeatInterval.TotalSeconds % 1 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heartbeatInterval),
                $"{nameof(heartbeatInterval)} must be a multiple of 1s, got {heartbeatInterval.TotalSeconds}");
        }

        if (heartbeatInterval >= HeartbeatTimeout)
        {
            throw new ArgumentException($"{nameof(heartbeatInterval)} must be less than {nameof(HeartbeatTimeout)}", nameof(heartbeatInterval));
        }

        HeartbeatInterval = heartbeatInterval;

        return this;
    }

    public HttpWebSocketsSignalReceiverConfiguration WithHeartbeatTimeout(TimeSpan heartbeatTimeout)
    {
        if (heartbeatTimeout.TotalSeconds % 1 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heartbeatTimeout),
                $"{nameof(heartbeatTimeout)} must be a multiple of 1s, got {heartbeatTimeout.TotalSeconds}");
        }

        if (heartbeatTimeout < HeartbeatInterval)
        {
            throw new ArgumentException($"{nameof(heartbeatTimeout)} must be greater than {nameof(HeartbeatInterval)}", nameof(heartbeatTimeout));
        }

        HeartbeatTimeout = heartbeatTimeout;

        return this;
    }

    public HttpWebSocketsSignalReceiverConfiguration WithReconnectDelayFunction(HttpWebSocketsSignalReceiverReconnectDelayFn retryDelayFn)
    {
        ReconnectDelayFn = retryDelayFn;

        return this;
    }

    internal HttpWebSocketsSignalReceiverConfiguration WithSignalCallback(Action<object>? signalCallback)
    {
        SignalCallback = signalCallback;

        return this;
    }

    internal HttpWebSocketsSignalReceiverConfiguration WithExceptionCallback(Action<Exception>? exceptionCallback)
    {
        ExceptionCallback = exceptionCallback;

        return this;
    }

    private async Task<WebSocket> DefaultWebSocketFactory(Uri address, CancellationToken cancellationToken)
    {
        var webSocket = new ClientWebSocket();

        // these options are not supported in Blazor
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create("browser")))
        {
            webSocket.Options.CollectHttpResponseDetails = true;
            webSocket.Options.KeepAliveInterval = HeartbeatInterval;
        }

        await webSocket.ConnectAsync(address, cancellationToken).ConfigureAwait(false);

        return webSocket;
    }
}

public delegate Task HttpWebSocketsSignalReceiverReconnectDelayFn(
    WebSocketCloseStatus closeStatus,
    int statusCode,
    Exception? exception,
    CancellationToken cancellationToken);
