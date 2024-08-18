using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Common;

internal sealed class StreamingServerWebSocket<TRequest, TItem> : IDisposable
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

    public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
    {
        return socket.Read("type", LookupMessageType, cancellationToken);

        static Type LookupMessageType(string discriminator) => discriminator switch
        {
            InitialRequestMessage<TRequest>.Discriminator => typeof(InitialRequestMessage<TRequest>),
            RequestNextItemMessage.Discriminator => typeof(RequestNextItemMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, null),
        };
    }

    public async Task<bool> SendMessage(TItem? message, CancellationToken cancellationToken)
    {
        return await socket.Send(new StreamingMessageEnvelope<TItem>(StreamingMessageEnvelope<TItem>.Discriminator, message), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SendError(string message, CancellationToken cancellationToken)
    {
        return await socket.Send(new ErrorMessage(ErrorMessage.Discriminator, message), cancellationToken).ConfigureAwait(false);
    }

    public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken).ConfigureAwait(false);
}

internal sealed record InitialRequestMessage<TRequest>(string Type, TRequest Payload)
{
    public const string Discriminator = "initial";
}

internal sealed record RequestNextItemMessage(string Type)
{
    public const string Discriminator = "next";
}
