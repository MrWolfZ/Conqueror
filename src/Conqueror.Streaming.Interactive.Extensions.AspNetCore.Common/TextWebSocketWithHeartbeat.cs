using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
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

        public event OnSocketClose? SocketClosed
        {
            add => socket.SocketClosed += value;
            remove => socket.SocketClosed -= value;
        }

        public async Task<string?> Receive(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

            var msg = await socket.Receive(cts.Token);

            if (msg == HeartbeatContent && !heartbeatTimeoutTimer.Change(heartbeatTimeout, Timeout.InfiniteTimeSpan))
            {
                throw new InvalidOperationException("failed to reset heartbeat timeout timer");
            }

            return msg;
        }

        public async Task Send(string message, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

            await socket.Send(message, cts.Token);
        }

        public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken);

        private async void OnSendHeartbeat(object? state) => await Send(HeartbeatContent, CancellationToken.None);

        private async void OnHeartbeatTimeout(object? state)
        {
            cancellationTokenSource.Cancel();
            await Close(CancellationToken.None);
        }
    }
}
