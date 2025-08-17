namespace Conqueror.Transport.FileSystem;

using System.Buffers;

internal readonly struct CharBuffer(int capacity) : IDisposable
{
    private readonly char[] buffer = ArrayPool<char>.Shared.Rent(capacity);

    public Span<char> Span => buffer.AsSpan()[..capacity];

    public Memory<char> Memory => buffer.AsMemory()[..capacity];

    public void Dispose() => ArrayPool<char>.Shared.Return(buffer);
}
