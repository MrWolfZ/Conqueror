using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.WebSockets;

internal sealed class WebSocketReadStream(ConquerorWebSocket socket) : Stream
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "disposed by caller")]
    private readonly ConquerorWebSocket socket = socket ?? throw new ArgumentNullException(nameof(socket));
    private bool endOfMessageOnNextRead;
    private bool endOfStream;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("This stream does not support seeking");

    public override void SetLength(long value) =>
        throw new NotSupportedException("This stream does not support setting length");

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("This stream does not support writing");

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Only async operations are supported");

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => ReadAsyncInternal(buffer, cancellationToken);

    private async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (endOfMessageOnNextRead)
        {
            endOfMessageOnNextRead = false;
            return 0;
        }

        if (endOfStream)
        {
            throw new WebSocketException(WebSocketError.InvalidState);
        }

        var receiveResult = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

        if (receiveResult.MessageType != WebSocketMessageType.Binary)
        {
            throw new WebSocketException(WebSocketError.InvalidMessageType, $"invalid message type; expected {WebSocketMessageType.Binary}, received {receiveResult.MessageType}");
        }

        endOfMessageOnNextRead = receiveResult is { Count: > 0, EndOfMessage: true };

        if (socket.CloseStatus.HasValue)
        {
            endOfStream = true;
        }

        return receiveResult.Count;
    }
}
