using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal sealed class InteractiveStreamingServerWebSocket<T> : IDisposable
        where T : notnull
    {
        private readonly JsonWebSocket socket;

        public InteractiveStreamingServerWebSocket(JsonWebSocket socket)
        {
            this.socket = socket;
        }

        public void Dispose()
        {
            socket.Dispose();
        }

        public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
        {
            return socket.Read("type", LookupMessageType, cancellationToken);

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
