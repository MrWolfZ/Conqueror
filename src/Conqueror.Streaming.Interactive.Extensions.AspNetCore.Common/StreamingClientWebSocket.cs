using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal sealed class StreamingClientWebSocket<T> : IDisposable
    {
        private readonly JsonWebSocket socket;

        public StreamingClientWebSocket(JsonWebSocket socket)
        {
            this.socket = socket;
        }

        public event OnSocketClose? SocketClosed
        {
            add => socket.SocketClosed += value;
            remove => socket.SocketClosed -= value;
        }

        public async Task<object?> Receive(CancellationToken cancellationToken)
        {
            return await socket.Receive("type", LookupMessageType, cancellationToken);

            static Type LookupMessageType(string discriminator) => discriminator switch
            {
                StreamingMessageEnvelope<T>.Discriminator => typeof(StreamingMessageEnvelope<T>),
                _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, null),
            };
        }

        public async Task RequestNextItem(CancellationToken cancellationToken)
        {
            await socket.Send(new RequestNextItemMessage(RequestNextItemMessage.Discriminator), cancellationToken);
        }

        public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken);

        public void Dispose()
        {
            socket.Dispose();
        }
    }

    internal sealed record StreamingMessageEnvelope<T>(string Type, T? Message)
    {
        public const string Discriminator = "envelope";
    }
}
