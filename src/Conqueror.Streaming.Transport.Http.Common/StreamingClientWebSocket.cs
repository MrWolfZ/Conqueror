namespace Conqueror.Streaming.Transport.Http.Common;

internal sealed class StreamingClientWebSocket<TRequest, TItem>(JsonWebSocket socket) : IDisposable
    where TRequest : class
{
    public void Dispose() => socket.Dispose();

    public IAsyncEnumerable<object> Read(CancellationToken cancellationToken)
    {
        return socket.Read("type", LookupMessageType, cancellationToken);

        static Type LookupMessageType(string discriminator)
        {
            return discriminator switch
            {
                StreamingMessageEnvelope<TItem>.Discriminator => typeof(StreamingMessageEnvelope<TItem>),
                ErrorMessage.Discriminator => typeof(ErrorMessage),
                _ => throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, message: null),
            };
        }
    }

    public Task<bool> SendInitialRequest(TRequest request, CancellationToken cancellationToken) =>
        socket.Send(
            new InitialRequestMessage<TRequest>(InitialRequestMessage<TRequest>.Discriminator, request),
            cancellationToken
        );

    public Task<bool> RequestNextItem(CancellationToken cancellationToken) =>
        socket.Send(new RequestNextItemMessage(RequestNextItemMessage.Discriminator), cancellationToken);

    public async Task Close(CancellationToken cancellationToken) =>
        await socket.Close(cancellationToken).ConfigureAwait(false);
}

internal sealed record StreamingMessageEnvelope<T>(string Type, T? Message)
{
#pragma warning disable RCS1158
    public const string Discriminator = "envelope";
#pragma warning restore RCS1158
}

internal sealed record ErrorMessage(string Type, string Message)
{
    public const string Discriminator = "error";
}
