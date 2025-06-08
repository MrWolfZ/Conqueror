using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.WebSockets;

internal sealed class ConquerorWebSocket : IAsyncDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly TimeSpan heartbeatInterval;
    private readonly TimeSpan heartbeatTimeout;
    private readonly Timer heartbeatTimeoutTimer;
    private readonly Timer heartbeatTimer;
    private readonly WebSocket socket;

    // small state machine to track whether someone is reading so that we can gracefully close the connection
    // 0 - idle
    // 1 - read in progress
    // 2 - close in progress
    private int readState;

    // small state machine to ensure that heartbeats and normal messages are never sent concurrently
    // 0 - idle
    // 1 - normal send in progress
    // 2 - heartbeat in progress
    private int sendState;

    public ConquerorWebSocket(WebSocket socket, TimeSpan heartbeatInterval, TimeSpan heartbeatTimeout)
    {
        this.socket = socket;
        this.heartbeatInterval = heartbeatInterval;
        this.heartbeatTimeout = heartbeatTimeout;

        heartbeatTimer = new(
            OnSendHeartbeat,
            null,
            heartbeatInterval == TimeSpan.Zero ? Timeout.InfiniteTimeSpan : heartbeatInterval,
            heartbeatInterval == TimeSpan.Zero ? Timeout.InfiniteTimeSpan : heartbeatInterval);

        heartbeatTimeoutTimer = new(
            OnHeartbeatTimeout,
            null,
            heartbeatInterval == TimeSpan.Zero ? Timeout.InfiniteTimeSpan : heartbeatTimeout,
            Timeout.InfiniteTimeSpan);

        WriteStream = new(this);
        ReadStream = new(this);
    }

    public WebSocketState State => socket.State;

    public WebSocketCloseStatus? CloseStatus => socket.CloseStatus;

    public WebSocketWriteStream WriteStream { get; }

    private WebSocketReadStream ReadStream { get; }

    public async ValueTask DisposeAsync()
    {
        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        cancellationTokenSource.Dispose();

        await Close(CancellationToken.None).ConfigureAwait(false);

        await heartbeatTimer.DisposeAsync().ConfigureAwait(false);
        await heartbeatTimeoutTimer.DisposeAsync().ConfigureAwait(false);
        await WriteStream.DisposeAsync().ConfigureAwait(false);
        await ReadStream.DisposeAsync().ConfigureAwait(false);
        socket.Dispose();
    }

    public async IAsyncEnumerable<Stream> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref readState, 1, 0) != 0)
        {
            // if someone is already reading, or we are closing the connection, we just break
            yield break;
        }

        while (socket.State is WebSocketState.Open)
        {
            // we read ahead on the socket with an empty buffer to be notified when
            // a new message is available
            var receiveResult = await socket.ReceiveAsync(Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);

            if (socket.CloseStatus.HasValue)
            {
                if (socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseOutputAsync(
                                    socket.CloseStatus.Value,
                                    socket.CloseStatusDescription,
                                    cancellationToken)
                                .ConfigureAwait(false);
                }

                yield break;
            }

            // any message counts as a heartbeat, so we reset the timer on any message
            _ = heartbeatTimeoutTimer.Change(heartbeatTimeout, Timeout.InfiniteTimeSpan);

            if (receiveResult.EndOfMessage)
            {
                // since we read 0 bytes ahead, if we are at the end of the message,
                // it means this was a heartbeat, and therefore we just continue reading
                continue;
            }

            yield return ReadStream;
        }

        _ = Interlocked.CompareExchange(ref readState, 0, 1);
    }

    public ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return socket.ReceiveAsync(buffer, cancellationToken);
    }

    public async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, bool endOfMessage, CancellationToken cancellationToken)
    {
        while (Interlocked.CompareExchange(ref sendState, 1, 0) > 1)
        {
            // there is a heartbeat in progress, so we yield
            await Task.Yield();
        }

        await socket.SendAsync(
                        buffer,
                        WebSocketMessageType.Binary,
                        endOfMessage,
                        cancellationToken)
                    .ConfigureAwait(false);

        if (endOfMessage)
        {
            _ = Interlocked.CompareExchange(ref sendState, 0, 1);

            // any message counts as a heartbeat, so we reset the timer on any message
            _ = heartbeatTimer.Change(heartbeatInterval, heartbeatInterval);
        }
    }

    private async Task Close(CancellationToken cancellationToken)
    {
        try
        {
            if (socket.State is WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(
                                socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                socket.CloseStatusDescription,
                                cancellationToken)
                            .ConfigureAwait(false);

                return;
            }

            if (socket.State is not WebSocketState.Open)
            {
                return;
            }

            await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            nameof(WebSocketCloseStatus.NormalClosure),
                            cancellationToken)
                        .ConfigureAwait(false);

            if (Interlocked.CompareExchange(ref readState, 2, 0) == 0)
            {
                while (socket.State is not WebSocketState.Closed)
                {
                    // nobody is reading, so we read to conclude the close handshake
                    _ = await socket.ReceiveAsync(Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or IOException { InnerException: ObjectDisposedException })
        {
            // if closing the connection fails due to a disposed object, we consider
            // the closing successful; this can, for example, happen with the ASP Core
            // `TestWebSocket` class which can throw this error when server and client
            // close the connection at the same time
        }
    }

    private async void OnSendHeartbeat(object? state)
    {
        try
        {
            if (socket.State is not WebSocketState.Open && socket.State is not WebSocketState.CloseReceived)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref sendState, 2, 0) != 0)
            {
                // a message is currently being sent, so we don't need the heartbeat
                return;
            }

            await socket.SendAsync(
                            Memory<byte>.Empty,
                            WebSocketMessageType.Binary,
                            true,
                            cancellationTokenSource.Token)
                        .ConfigureAwait(false);

            _ = Interlocked.CompareExchange(ref sendState, 0, 2);
        }
        catch
        {
            // for now, we do nothing in this case; in the future we may want to close the connection
            // or send a message to the client indicating that the heartbeat failed
        }
    }

    private async void OnHeartbeatTimeout(object? state)
    {
        try
        {
            await Close(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // we can't really do anything here, so we just ignore any errors that may occur
            // during the cancellation or closing of the connection
        }
    }
}
