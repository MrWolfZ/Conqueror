namespace Conqueror.Streaming.Transport.Http.Common;

using System.Runtime.CompilerServices;

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

        heartbeatTimer = new Timer(OnSendHeartbeat, state: null, Timeout.InfiniteTimeSpan, heartbeatInterval);
        heartbeatTimeoutTimer = new Timer(OnHeartbeatTimeout, state: null, heartbeatTimeout, Timeout.InfiniteTimeSpan);
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
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            cancellationTokenSource.Token
        );

        await foreach (var msg in socket.Read(cts.Token).ConfigureAwait(false))
        {
            if (
                string.Equals(msg, HeartbeatContent, StringComparison.Ordinal)
                && !heartbeatTimeoutTimer.Change(heartbeatTimeout, Timeout.InfiniteTimeSpan)
            )
            {
                throw new InvalidOperationException("failed to reset heartbeat timeout timer");
            }

            yield return msg;
        }
    }

    public async Task<bool> Send(string message, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            cancellationTokenSource.Token
        );

        return await socket.Send(message, cts.Token).ConfigureAwait(false);
    }

    public async Task Close(CancellationToken cancellationToken) =>
        await socket.Close(cancellationToken).ConfigureAwait(false);

    private void OnSendHeartbeat(object? state) =>
        _ = Send(HeartbeatContent, CancellationToken.None).ConfigureAwait(false);

    private void OnHeartbeatTimeout(object? state)
    {
#pragma warning disable MA0045
        cancellationTokenSource.Cancel();
#pragma warning restore MA0045
        _ = Close(CancellationToken.None).ConfigureAwait(false);
    }
}
