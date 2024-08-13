using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Common;

internal sealed class TextWebSocketWithHeartbeat : IDisposable
{
    private const string HeartbeatContent = "ping";
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly TimeSpan heartbeatTimeout;
    private readonly Timer heartbeatTimeoutTimer;
    private readonly Timer heartbeatTimer;
    private readonly TextWebSocket socket;

    public TextWebSocketWithHeartbeat(TextWebSocket socket, TimeSpan heartbeatInterval, TimeSpan heartbeatTimeout)
    {
        this.socket = socket;
        this.heartbeatTimeout = heartbeatTimeout;

        heartbeatTimer = new(OnSendHeartbeat, null, Timeout.InfiniteTimeSpan, heartbeatInterval);
        heartbeatTimeoutTimer = new(OnHeartbeatTimeout, null, heartbeatTimeout, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        socket.Dispose();
        heartbeatTimer.Dispose();
        heartbeatTimeoutTimer.Dispose();
        cancellationTokenSource.Dispose();
    }

    public async IAsyncEnumerable<string> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

        await foreach (var msg in socket.Read(cts.Token))
        {
            if (msg == HeartbeatContent && !heartbeatTimeoutTimer.Change(heartbeatTimeout, Timeout.InfiniteTimeSpan))
            {
                throw new InvalidOperationException("failed to reset heartbeat timeout timer");
            }

            yield return msg;
        }
    }

    public async Task<bool> Send(string message, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

        return await socket.Send(message, cts.Token).ConfigureAwait(false);
    }

    public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken).ConfigureAwait(false);

    private async void OnSendHeartbeat(object? state) => await Send(HeartbeatContent, CancellationToken.None).ConfigureAwait(false);

    private async void OnHeartbeatTimeout(object? state)
    {
        cancellationTokenSource.Cancel();
        await Close(CancellationToken.None).ConfigureAwait(false);
    }
}
