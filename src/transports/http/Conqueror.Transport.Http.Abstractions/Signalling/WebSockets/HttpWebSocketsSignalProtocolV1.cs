namespace Conqueror.Signalling.WebSockets;

internal static class HttpWebSocketsSignalProtocolV1
{
    private const int Version = 1;

    public static Task Write(
        Stream stream,
        string tag,
        string? contextData,
        Func<Stream, CancellationToken, ValueTask> writeSignal,
        in CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(writeSignal);

        return WriteInner(stream, tag, contextData, writeSignal, cancellationToken);
    }

    public static async Task<(object Signal, string? ContextData)> Read(
        Stream stream,
        Func<string, Stream, CancellationToken, ValueTask<object>> readSignalByTag,
        CancellationToken cancellationToken
    )
    {
        var byteBuffer = new byte[4];

        await stream.ReadExactlyAsync(byteBuffer, offset: 0, count: 1, cancellationToken).ConfigureAwait(false);

        var versionAndFlags = byteBuffer[0];

        var version = (versionAndFlags & 0xF0) >> 4;

        // int flags = versionAndFlags & 0x0F; // not used yet

        if (version != Version)
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Unsupported protocol version: {version}")
            );
        }

        var remainingHeaderLength = await ReadUInt24(stream, byteBuffer, cancellationToken).ConfigureAwait(false);

        // expect at least 2 bytes for tag length
        if (remainingHeaderLength < 2)
        {
            throw new InvalidOperationException("Invalid header length");
        }

        using var headerBuffer = MemoryPool<byte>.Shared.Rent(remainingHeaderLength);
        var headerMemory = headerBuffer.Memory[..remainingHeaderLength];

        await stream.ReadExactlyAsync(headerMemory, cancellationToken).ConfigureAwait(false);

        var tagLength = (ushort)((headerMemory.Span[0] << 8) | headerMemory.Span[1]);
        var tag = Encoding.UTF8.GetString(headerMemory.Slice(start: 2, tagLength).Span);

        var contextMemory = headerMemory[(2 + tagLength)..];

        string? contextData = null;
        if (contextMemory.Length > 0)
        {
            contextData = Encoding.UTF8.GetString(contextMemory.Span);
        }

        var signal = await readSignalByTag(tag, stream, cancellationToken).ConfigureAwait(false);

        return (signal, contextData);
    }

    private static async Task WriteInner(
        Stream stream,
        string tag,
        string? contextData,
        Func<Stream, CancellationToken, ValueTask> writeSignal,
        CancellationToken cancellationToken
    )
    {
        var tagBytes = Encoding.UTF8.GetBytes(tag);
        var tagLength = tagBytes.Length;

        var contextBytes = contextData is not null ? Encoding.UTF8.GetBytes(contextData) : [];
        var contextLength = contextBytes.Length;

        var byteBuffer = new byte[4];

        // Version (4 bits) + Flags (4 bits)
        const byte versionAndFlags = (Version & 0x0F) << 4; // flags = 0

        byteBuffer[0] = versionAndFlags;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        // 3-byte header length
        var remainingHeaderLength = 2 + tagLength + contextLength;

        await WriteUInt24(stream, byteBuffer, remainingHeaderLength, cancellationToken).ConfigureAwait(false);

        // tag
        await WriteUInt16(stream, byteBuffer, tagLength, cancellationToken).ConfigureAwait(false);

        await stream.WriteAsync(tagBytes.AsMemory(), cancellationToken).ConfigureAwait(false);

        // context data
        await stream.WriteAsync(contextBytes.AsMemory(), cancellationToken).ConfigureAwait(false);

        // signal content
        await writeSignal(stream, cancellationToken).ConfigureAwait(false);

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteUInt16(Stream stream, byte[] buffer, int value, CancellationToken cancellationToken)
    {
        if (value > 0xFFFF)
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"value too large (max {0xFFFF}, got {value})")
            );
        }

        buffer[0] = (byte)((value >> 8) & 0xFF);
        buffer[1] = (byte)(value & 0xFF);
        await stream.WriteAsync(buffer.AsMemory()[..2], cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteUInt24(Stream stream, byte[] buffer, int value, CancellationToken cancellationToken)
    {
        if (value > 0xFFFFFF)
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"value too large (max {0xFFFFFF}, got {value})")
            );
        }

        buffer[0] = (byte)((value >> 16) & 0xFF);
        buffer[1] = (byte)((value >> 8) & 0xFF);
        buffer[2] = (byte)(value & 0xFF);
        await stream.WriteAsync(buffer.AsMemory()[..3], cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ReadUInt24(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        await stream.ReadExactlyAsync(buffer, offset: 0, count: 3, cancellationToken).ConfigureAwait(false);

        return (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
    }
}
