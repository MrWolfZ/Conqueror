namespace Conqueror.Streaming.Transport.Http.Common;

internal sealed class StreamingServerWebSocket<TRequest, TItem>(JsonWebSocket socket) : IDisposable
{
    public void Dispose() => socket.Dispose();

    public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
    {
        return socket.Read("type", LookupMessageType, cancellationToken);

        static Type LookupMessageType(string discriminator)
        {
            return discriminator switch
            {
                InitialRequestMessage<TRequest>.Discriminator => typeof(InitialRequestMessage<TRequest>),
                RequestNextItemMessage.Discriminator => typeof(RequestNextItemMessage),
                _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, message: null),
            };
        }
    }

    public Task<bool> SendMessage(TItem? message, CancellationToken cancellationToken) =>
        socket.Send(
            new StreamingMessageEnvelope<TItem>(StreamingMessageEnvelope<TItem>.Discriminator, message),
            cancellationToken
        );

    public Task<bool> SendError(string message, CancellationToken cancellationToken) =>
        socket.Send(new ErrorMessage(ErrorMessage.Discriminator, message), cancellationToken);

    public async Task Close(CancellationToken cancellationToken) =>
        await socket.Close(cancellationToken).ConfigureAwait(false);
}

internal sealed record InitialRequestMessage<TRequest>(string Type, TRequest Payload)
{
#pragma warning disable RCS1158
    public const string Discriminator = "initial";
#pragma warning restore RCS1158
}

internal sealed record RequestNextItemMessage(string Type)
{
    public const string Discriminator = "next";
}
