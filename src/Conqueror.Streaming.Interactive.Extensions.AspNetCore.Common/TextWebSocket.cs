using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal sealed class TextWebSocket : IDisposable
    {
        private readonly WebSocket socket;
        private readonly CancellationTokenSource cancellationTokenSource = new();

        public TextWebSocket(WebSocket socket)
        {
            this.socket = socket;
        }

        public void Dispose()
        {
            socket.Dispose();
            cancellationTokenSource.Dispose();
        }

        public async IAsyncEnumerable<string> Read([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

            var buffer = new byte[1024 * 4];

            while (true)
            {
                var receiveResult = await socket.ReceiveAsync(buffer, cts.Token);

                if (receiveResult.CloseStatus.HasValue)
                {
                    yield break;
                }

                if (receiveResult.MessageType != WebSocketMessageType.Text)
                {
                    throw new InvalidOperationException($"expected websocket message type '{WebSocketMessageType.Text}', got '{receiveResult.MessageType}'");
                }

                if (receiveResult.EndOfMessage)
                {
                    yield return Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    continue;
                }

                var allBytes = new List<byte>();

                allBytes.AddRange(new ArraySegment<byte>(buffer, 0, receiveResult.Count));

                while (!receiveResult.EndOfMessage)
                {
                    receiveResult = await socket.ReceiveAsync(buffer, cts.Token);
                    allBytes.AddRange(new ArraySegment<byte>(buffer, 0, receiveResult.Count));
                }

                yield return Encoding.UTF8.GetString(allBytes.ToArray());
            }
        }

        public async Task Send(string message, CancellationToken cancellationToken)
        {
            if (socket.State is not WebSocketState.Open && socket.State is not WebSocketState.CloseReceived)
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
            if (socket.State is not WebSocketState.Open && socket.State is not WebSocketState.CloseReceived && socket.State is not WebSocketState.CloseSent)
            {
                return;
            }
            
            cancellationTokenSource.Cancel();

            try
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    nameof(WebSocketCloseStatus.NormalClosure),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or IOException { InnerException: ObjectDisposedException })
            {
                // if closing the connection fails due to a disposed object, we consider
                // the closing successful; this can for example happen with the ASP Core
                // `TestWebSocket` class which can throw this error when server and client
                // close the connection at the same time
            }
        }
    }
}
