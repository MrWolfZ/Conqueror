using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal delegate void OnSocketClose();

    internal sealed class TextWebSocket : IDisposable
    {
        private readonly WebSocket socket;

        public TextWebSocket(WebSocket socket)
        {
            this.socket = socket;
        }

        public void Dispose()
        {
            socket.Dispose();
        }

        public event OnSocketClose? SocketClosed;

        public async Task<string?> Receive(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await socket.ReceiveAsync(buffer, cancellationToken);

            if (receiveResult.CloseStatus.HasValue)
            {
                SocketClosed?.Invoke();
                return null;
            }

            if (receiveResult.EndOfMessage)
            {
                return Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            }

            var allBytes = new List<byte>();

            allBytes.AddRange(new ArraySegment<byte>(buffer, 0, receiveResult.Count));

            while (!receiveResult.EndOfMessage)
            {
                receiveResult = await socket.ReceiveAsync(buffer, cancellationToken);
                allBytes.AddRange(new ArraySegment<byte>(buffer, 0, receiveResult.Count));
            }

            return Encoding.UTF8.GetString(allBytes.ToArray());
        }

        public async Task Send(string message, CancellationToken cancellationToken)
        {
            if (socket.State != WebSocketState.Open && socket.State != WebSocketState.CloseReceived)
            {
                return;
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);

            await socket.SendAsync(
                new(messageBytes, 0, messageBytes.Length),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }

        public async Task Close(CancellationToken cancellationToken)
        {
            if (socket.State != WebSocketState.Open && socket.State != WebSocketState.CloseReceived && socket.State != WebSocketState.CloseSent)
            {
                return;
            }

            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                nameof(WebSocketCloseStatus.NormalClosure),
                cancellationToken);
        }
    }
}
