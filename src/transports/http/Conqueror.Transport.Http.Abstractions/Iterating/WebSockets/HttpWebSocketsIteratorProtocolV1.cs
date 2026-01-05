namespace Conqueror.Iterating.WebSockets;

internal static class HttpWebSocketsIteratorProtocolV1
{
    private const int Version = 1;

    public enum ClientMessageType : byte
    {
        InitiateIteration = 0,
        FetchMore = 1,
    }

    public enum ServerMessageType : byte
    {
        Items = 0,
        Completion = 1,
        Error = 2,
    }

    public static Task WriteInitiateIteration(
        Stream stream,
        int prefetchCount,
        string? contextData,
        Func<Stream, CancellationToken, ValueTask> writeIterator,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(writeIterator);

        if (prefetchCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(prefetchCount),
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{nameof(prefetchCount)} must be at least 1, got {prefetchCount}"
                )
            );
        }

        return WriteInitiateIterationInner(stream, prefetchCount, contextData, writeIterator, cancellationToken);
    }

    public static Task WriteFetchMore(Stream stream, int prefetchCount, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (prefetchCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(prefetchCount),
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{nameof(prefetchCount)} must be at least 1, got {prefetchCount}"
                )
            );
        }

        return WriteFetchMoreInner(stream, prefetchCount, cancellationToken);
    }

    public static async Task<ClientMessage> ReadClientMessage(
        Stream stream,
        Func<Stream, CancellationToken, ValueTask<object>> readIterator,
        CancellationToken cancellationToken
    )
    {
        var byteBuffer = new byte[4];

        await stream.ReadExactlyAsync(byteBuffer, offset: 0, count: 1, cancellationToken).ConfigureAwait(false);

        var versionAndFlags = byteBuffer[0];
        var version = (versionAndFlags & 0xF0) >> 4;

        if (version != Version)
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Unsupported protocol version: {version}")
            );
        }

        var remainingHeaderLength = await ReadUInt24(stream, byteBuffer, cancellationToken).ConfigureAwait(false);

        if (remainingHeaderLength < 1)
        {
            throw new InvalidOperationException("Invalid header length");
        }

        using var headerBuffer = MemoryPool<byte>.Shared.Rent(remainingHeaderLength);
        var headerMemory = headerBuffer.Memory[..remainingHeaderLength];

        await stream.ReadExactlyAsync(headerMemory, cancellationToken).ConfigureAwait(false);

        var messageType = (ClientMessageType)headerMemory.Span[0];

        return messageType switch
        {
            ClientMessageType.InitiateIteration => await ReadInitiateIterationMessage(
                    stream,
                    headerMemory[1..],
                    readIterator,
                    cancellationToken
                )
                .ConfigureAwait(false),
            ClientMessageType.FetchMore => ReadFetchMoreMessage(headerMemory[1..]),
            _ => throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Unknown client message type: {messageType}")
            ),
        };
    }

    public static Task WriteItems(
        Stream stream,
        IReadOnlyCollection<Action<Stream, CancellationToken>> itemWriters,
        string? contextData,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(itemWriters);

        if (itemWriters.Count is 0)
        {
            throw new ArgumentException("Must provide at least one item to write", nameof(itemWriters));
        }

        return WriteItemsInner(stream, itemWriters, contextData, cancellationToken);
    }

    public static Task WriteCompletion(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return WriteCompletionInner(stream, cancellationToken);
    }

    public static Task WriteError(Stream stream, string errorMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(errorMessage);

        return WriteErrorInner(stream, errorMessage, cancellationToken);
    }

    public static async Task<ServerMessage> ReadServerMessage(
        Stream stream,
        Func<Stream, CancellationToken, ValueTask<object>> readItem,
        CancellationToken cancellationToken
    )
    {
        var byteBuffer = new byte[4];

        await stream.ReadExactlyAsync(byteBuffer, offset: 0, count: 1, cancellationToken).ConfigureAwait(false);

        var versionAndFlags = byteBuffer[0];
        var version = (versionAndFlags & 0xF0) >> 4;

        if (version != Version)
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Unsupported protocol version: {version}")
            );
        }

        var remainingHeaderLength = await ReadUInt24(stream, byteBuffer, cancellationToken).ConfigureAwait(false);

        if (remainingHeaderLength < 1)
        {
            throw new InvalidOperationException("Invalid header length");
        }

        using var headerBuffer = MemoryPool<byte>.Shared.Rent(remainingHeaderLength);
        var headerMemory = headerBuffer.Memory[..remainingHeaderLength];

        await stream.ReadExactlyAsync(headerMemory, cancellationToken).ConfigureAwait(false);

        var messageType = (ServerMessageType)headerMemory.Span[0];

        return messageType switch
        {
            ServerMessageType.Items => await ReadItemsMessage(stream, headerMemory[1..], readItem, cancellationToken)
                .ConfigureAwait(false),
            ServerMessageType.Completion => new CompletionMessage(),
            ServerMessageType.Error => ReadErrorMessage(headerMemory[1..]),
            _ => throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Unknown server message type: {messageType}")
            ),
        };
    }

    private static async Task WriteInitiateIterationInner(
        Stream stream,
        int prefetchCount,
        string? contextData,
        Func<Stream, CancellationToken, ValueTask> writeIterator,
        CancellationToken cancellationToken
    )
    {
        var contextBytes = contextData is not null ? Encoding.UTF8.GetBytes(contextData) : [];
        var contextLength = contextBytes.Length;

        var byteBuffer = new byte[4];

        const byte versionAndFlags = (Version & 0x0F) << 4;
        byteBuffer[0] = versionAndFlags;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        var remainingHeaderLength = 1 + 4 + 2 + contextLength;
        await WriteUInt24(stream, byteBuffer, remainingHeaderLength, cancellationToken).ConfigureAwait(false);

        byteBuffer[0] = (byte)ClientMessageType.InitiateIteration;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        await WriteInt32(stream, byteBuffer, prefetchCount, cancellationToken).ConfigureAwait(false);

        await WriteUInt16(stream, byteBuffer, contextLength, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(contextBytes.AsMemory(), cancellationToken).ConfigureAwait(false);

        await writeIterator(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteFetchMoreInner(Stream stream, int prefetchCount, CancellationToken cancellationToken)
    {
        var byteBuffer = new byte[4];

        const byte versionAndFlags = (Version & 0x0F) << 4;
        byteBuffer[0] = versionAndFlags;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        const int remainingHeaderLength = 1 + 4;
        await WriteUInt24(stream, byteBuffer, remainingHeaderLength, cancellationToken).ConfigureAwait(false);

        byteBuffer[0] = (byte)ClientMessageType.FetchMore;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        await WriteInt32(stream, byteBuffer, prefetchCount, cancellationToken).ConfigureAwait(false);

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<InitiateIterationMessage> ReadInitiateIterationMessage(
        Stream stream,
        ReadOnlyMemory<byte> headerMemory,
        Func<Stream, CancellationToken, ValueTask<object>> readIterator,
        CancellationToken cancellationToken
    )
    {
        if (headerMemory.Length < 6)
        {
            throw new InvalidOperationException("Invalid InitiateIteration message header");
        }

        var prefetchCount = ReadInt32FromSpan(headerMemory.Span);
        var contextLength = ReadUInt16FromSpan(headerMemory[4..].Span);

        string? contextData = null;
        if (contextLength > 0)
        {
            var contextMemory = headerMemory.Slice(start: 6, contextLength);
            contextData = Encoding.UTF8.GetString(contextMemory.Span);
        }

        var iterator = await readIterator(stream, cancellationToken).ConfigureAwait(false);

        return new InitiateIterationMessage(prefetchCount, contextData, iterator);
    }

    private static FetchMoreMessage ReadFetchMoreMessage(ReadOnlyMemory<byte> headerMemory)
    {
        if (headerMemory.Length < 4)
        {
            throw new InvalidOperationException("Invalid FetchMore message header");
        }

        var prefetchCount = ReadInt32FromSpan(headerMemory.Span);
        return new FetchMoreMessage(prefetchCount);
    }

    private static async Task WriteItemsInner(
        Stream stream,
        IReadOnlyCollection<Action<Stream, CancellationToken>> writeItems,
        string? contextData,
        CancellationToken cancellationToken
    )
    {
        var contextBytes = contextData is not null ? Encoding.UTF8.GetBytes(contextData) : [];
        var contextLength = contextBytes.Length;

        var byteBuffer = new byte[4];

        const byte versionAndFlags = (Version & 0x0F) << 4;
        byteBuffer[0] = versionAndFlags;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        var remainingHeaderLength = 1 + 4 + 2 + contextLength;
        await WriteUInt24(stream, byteBuffer, remainingHeaderLength, cancellationToken).ConfigureAwait(false);

        byteBuffer[0] = (byte)ServerMessageType.Items;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        await WriteInt32(stream, byteBuffer, writeItems.Count, cancellationToken).ConfigureAwait(false);

        await WriteUInt16(stream, byteBuffer, contextLength, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(contextBytes.AsMemory(), cancellationToken).ConfigureAwait(false);

        foreach (var writeItem in writeItems)
        {
            writeItem(stream, cancellationToken);
        }

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteCompletionInner(Stream stream, CancellationToken cancellationToken)
    {
        var byteBuffer = new byte[4];

        const byte versionAndFlags = (Version & 0x0F) << 4;
        byteBuffer[0] = versionAndFlags;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        const int remainingHeaderLength = 1;
        await WriteUInt24(stream, byteBuffer, remainingHeaderLength, cancellationToken).ConfigureAwait(false);

        byteBuffer[0] = (byte)ServerMessageType.Completion;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteErrorInner(Stream stream, string errorMessage, CancellationToken cancellationToken)
    {
        var errorBytes = Encoding.UTF8.GetBytes(errorMessage);
        var errorLength = errorBytes.Length;

        var byteBuffer = new byte[4];

        const byte versionAndFlags = (Version & 0x0F) << 4;
        byteBuffer[0] = versionAndFlags;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        var remainingHeaderLength = 1 + 2 + errorLength;
        await WriteUInt24(stream, byteBuffer, remainingHeaderLength, cancellationToken).ConfigureAwait(false);

        byteBuffer[0] = (byte)ServerMessageType.Error;
        await stream.WriteAsync(byteBuffer.AsMemory()[..1], cancellationToken).ConfigureAwait(false);

        await WriteUInt16(stream, byteBuffer, errorLength, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(errorBytes.AsMemory(), cancellationToken).ConfigureAwait(false);

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ItemsMessage> ReadItemsMessage(
        Stream stream,
        ReadOnlyMemory<byte> headerMemory,
        Func<Stream, CancellationToken, ValueTask<object>> readItem,
        CancellationToken cancellationToken
    )
    {
        if (headerMemory.Length < 6)
        {
            throw new InvalidOperationException("Invalid Items message header");
        }

        var itemCount = ReadInt32FromSpan(headerMemory.Span);
        var contextLength = ReadUInt16FromSpan(headerMemory[4..].Span);

        string? contextData = null;
        if (contextLength > 0)
        {
            var contextMemory = headerMemory.Slice(start: 6, contextLength);
            contextData = Encoding.UTF8.GetString(contextMemory.Span);
        }

        var items = new List<object>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            var item = await readItem(stream, cancellationToken).ConfigureAwait(false);
            items.Add(item);
        }

        return new ItemsMessage(items, contextData);
    }

    private static ErrorMessage ReadErrorMessage(ReadOnlyMemory<byte> headerMemory)
    {
        if (headerMemory.Length < 2)
        {
            throw new InvalidOperationException("Invalid Error message header");
        }

        var errorLength = ReadUInt16FromSpan(headerMemory.Span);
        var message = Encoding.UTF8.GetString(headerMemory.Slice(start: 2, errorLength).Span);

        return new ErrorMessage(message);
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

    private static async Task WriteInt32(Stream stream, byte[] buffer, int value, CancellationToken cancellationToken)
    {
        buffer[0] = (byte)((value >> 24) & 0xFF);
        buffer[1] = (byte)((value >> 16) & 0xFF);
        buffer[2] = (byte)((value >> 8) & 0xFF);
        buffer[3] = (byte)(value & 0xFF);
        await stream.WriteAsync(buffer.AsMemory()[..4], cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ReadUInt24(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        await stream.ReadExactlyAsync(buffer, offset: 0, count: 3, cancellationToken).ConfigureAwait(false);

        return (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
    }

    private static int ReadInt32FromSpan(ReadOnlySpan<byte> span) =>
        (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];

    private static int ReadUInt16FromSpan(ReadOnlySpan<byte> span) => (span[0] << 8) | span[1];

    public abstract record ClientMessage;

    public sealed record InitiateIterationMessage(int PrefetchCount, string? ContextData, object Iterator)
        : ClientMessage;

    public sealed record FetchMoreMessage(int PrefetchCount) : ClientMessage;

    public abstract record ServerMessage;

    public sealed record ItemsMessage(IReadOnlyCollection<object> Items, string? ContextData) : ServerMessage;

    public sealed record CompletionMessage : ServerMessage;

    public sealed record ErrorMessage(string Message) : ServerMessage;
}
