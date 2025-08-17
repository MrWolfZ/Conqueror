namespace Conqueror.Transport.Http.Client.WebSockets;

using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;

internal sealed class WebSocketWriteStream(ConquerorWebSocket socket) : Stream
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "disposed by caller")]
    private readonly ConquerorWebSocket socket = socket ?? throw new ArgumentNullException(nameof(socket));

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() =>
        throw new NotSupportedException("This stream does not support synchronous flushing");

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        if (socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived)
        {
            return Task.CompletedTask;
        }

        return socket.SendAsync(Memory<byte>.Empty, endOfMessage: true, cancellationToken).AsTask();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("This stream does not support reading");

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("This stream does not support seeking");

    public override void SetLength(long value) =>
        throw new NotSupportedException("This stream does not support setting length");

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("This stream does not support synchronous writing");

    public override void Write(ReadOnlySpan<byte> buffer) =>
        throw new NotSupportedException("This stream does not support synchronous writing");

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived)
        {
            return default;
        }

        return socket.SendAsync(buffer, endOfMessage: false, cancellationToken);
    }
}
