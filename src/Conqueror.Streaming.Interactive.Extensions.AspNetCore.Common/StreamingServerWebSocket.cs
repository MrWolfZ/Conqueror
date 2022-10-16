using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal sealed class StreamingServerWebSocket<T> : IDisposable
        where T : notnull
    {
        private readonly JsonWebSocket socket;

        public StreamingServerWebSocket(JsonWebSocket socket)
        {
            this.socket = socket;
        }

        public void Dispose()
        {
            socket.Dispose();
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
                RequestNextItemMessage.Discriminator => typeof(RequestNextItemMessage),
                _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, null),
            };
        }

        public async Task SendMessage(T? message, CancellationToken cancellationToken)
        {
            await socket.Send(new StreamingMessageEnvelope<T>(StreamingMessageEnvelope<T>.Discriminator, message), cancellationToken);
        }

        public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken);
    }

    internal sealed record RequestNextItemMessage(string Type)
    {
        public const string Discriminator = "next";
    }
}
