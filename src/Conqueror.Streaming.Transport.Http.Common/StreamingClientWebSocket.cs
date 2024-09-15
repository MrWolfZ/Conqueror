using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Common;

internal sealed class StreamingClientWebSocket<TRequest, TItem>(JsonWebSocket socket) : IDisposable
    where TRequest : class
{
    public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
    {
        return socket.Read("type", LookupMessageType, cancellationToken);

        static Type LookupMessageType(string discriminator) => discriminator switch
        {
            StreamingMessageEnvelope<TItem>.Discriminator => typeof(StreamingMessageEnvelope<TItem>),
            ErrorMessage.Discriminator => typeof(ErrorMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, null),
        };
    }

    public async Task<bool> SendInitialRequest(TRequest request, CancellationToken cancellationToken)
    {
        return await socket.Send(new InitialRequestMessage<TRequest>(InitialRequestMessage<TRequest>.Discriminator, request), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RequestNextItem(CancellationToken cancellationToken)
    {
        return await socket.Send(new RequestNextItemMessage(RequestNextItemMessage.Discriminator), cancellationToken).ConfigureAwait(false);
    }

    public async Task Close(CancellationToken cancellationToken) => await socket.Close(cancellationToken).ConfigureAwait(false);

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
