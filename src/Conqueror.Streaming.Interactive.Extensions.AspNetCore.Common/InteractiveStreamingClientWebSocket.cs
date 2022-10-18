﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common
{
    internal sealed class InteractiveStreamingClientWebSocket<T> : IDisposable
    {
        private readonly JsonWebSocket socket;

        public InteractiveStreamingClientWebSocket(JsonWebSocket socket)
        {
            this.socket = socket;
        }

        public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
        {
            return socket.Read("type", LookupMessageType, cancellationToken);

            static Type LookupMessageType(string discriminator) => discriminator switch
            {
                StreamingMessageEnvelope<T>.Discriminator => typeof(StreamingMessageEnvelope<T>),
                ErrorMessage.Discriminator => typeof(ErrorMessage),
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

    internal sealed record ErrorMessage(string Type, string Message)
    {
        public const string Discriminator = "error";
    }
}
