namespace Conqueror.Transport.Http.Server.AspNetCore;

internal sealed class MultiplexWriteStream(IReadOnlyCollection<Stream> streams) : Stream
{
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

    public override async Task FlushAsync(CancellationToken cancellationToken) =>
        await Task.WhenAll(streams.Select(s => s.FlushAsync(cancellationToken))).ConfigureAwait(false);

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

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default
    ) =>
        await Task.WhenAll(streams.Select(s => s.WriteAsync(buffer, cancellationToken).AsTask())).ConfigureAwait(false);
}
