using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Conqueror.Transport.FileSystem;

internal static class FileOperations
{
    public static bool FileExists(this FilePath filePath) => File.Exists(filePath);

    public static long GetLength(this FilePath filePath) => new FileInfo(filePath).Length;

    public static async Task<ReadOnlyFileHandle> OpenRead(
        this FilePath filePath,
        CancellationToken cancellationToken)
    {
        var fileStream = await filePath.OpenWithRetry(FileAccess.Read, cancellationToken).ConfigureAwait(false);

        return new(filePath, fileStream);
    }

    public static async Task<ReadWriteFileHandle> OpenReadWrite(
        this FilePath filePath,
        CancellationToken cancellationToken)
    {
        var fileStream = await filePath.OpenWithRetry(FileAccess.ReadWrite, cancellationToken).ConfigureAwait(false);

        return new(filePath, fileStream);
    }

    public static async Task WriteJson<T>(
        this ReadWriteFileHandle handle,
        T value,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(
                                handle.Stream,
                                value,
                                jsonTypeInfo,
                                cancellationToken)
                            .ConfigureAwait(false);
    }

    public static async Task<T> ReadJson<T>(
        this ReadWriteFileHandle handle,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var result = await JsonSerializer.DeserializeAsync(
                                             handle.Stream,
                                             jsonTypeInfo,
                                             cancellationToken)
                                         .ConfigureAwait(false);

        return result ?? throw new IOException($"failed to JSON-deserialize file '{handle.FilePath}' to object of type '{typeof(T)}'");
    }

    public static async Task DeleteLineFromFile(
        ReadWriteFileHandle handle,
        int lineNumber,
        int lineLength,
        CancellationToken cancellationToken)
    {
        if (lineNumber < 0 || lineLength <= 0)
        {
            throw new ArgumentException("Invalid line number or line length.");
        }

        var stream = handle.Stream;
        var totalLines = stream.Length / lineLength;

        if (lineNumber >= totalLines)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number exceeds file line count.");
        }

        long readPos = (lineNumber + 1) * lineLength;
        long writePos = lineNumber * lineLength;

        var buffer = new byte[lineLength];

        // Shift all lines after the deleted one forward
        while (readPos < stream.Length)
        {
            _ = stream.Seek(readPos, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, lineLength), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            _ = stream.Seek(writePos, SeekOrigin.Begin);
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

            readPos += lineLength;
            writePos += lineLength;
        }

        // Truncate the file to remove the last line
        stream.SetLength(stream.Length - lineLength);
    }

    private static async ValueTask<FileStream> OpenWithRetry(
        this FilePath filePath,
        FileAccess access,
        CancellationToken cancellationToken,
        FileShare share = FileShare.Read,
        int maxAttempts = 15,
        int initialDelayMs = 0,
        int maxDelayMs = 1000)
    {
        var attempt = 0;
        var delayMs = initialDelayMs;

        while (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
        {
            try
            {
                return new(
                    filePath,
                    FileMode.OpenOrCreate,
                    access,
                    share);
            }
            catch (IOException)
            {
                // File is likely in use
                attempt += 1;

                if (attempt >= maxAttempts)
                {
                    throw new IOException($"Could not acquire lock on file '{filePath}' after {maxAttempts} attempts.");
                }

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Yield(); // Yield to other threads to avoid busy-waiting.
                }

                // we try to acquire the file lock a few times without delay before we start backing off
                if (attempt >= 5)
                {
                    // Exponential backoff with cap
                    delayMs = delayMs == 0 ? 10 : Math.Min(delayMs * 2, maxDelayMs);
                }
            }
        }

        // Should not reach here
        throw new IOException("Failed to open file after retries.");
    }
}

internal sealed class ReadOnlyFileHandle(FilePath path, FileStream stream) : IAsyncDisposable
{
    private StreamReader? reader;

    public FilePath FilePath => path;

    public FileStream Stream => stream;

    public StreamReader Reader => reader ??= new(Stream, leaveOpen: true);

    public async ValueTask DisposeAsync()
    {
        reader?.Dispose();

        await Stream.DisposeAsync().ConfigureAwait(false);
    }
}

internal sealed class ReadWriteFileHandle(FilePath path, FileStream stream) : IAsyncDisposable
{
    private StreamReader? reader;
    private StreamWriter? writer;

    public FilePath FilePath => path;

    public FileStream Stream => stream;

    public StreamReader Reader => reader ??= new(Stream, leaveOpen: true);

    public StreamWriter Writer => writer ??= new(Stream, leaveOpen: true);

    public async ValueTask DisposeAsync()
    {
        if (writer is not null)
        {
            await writer.DisposeAsync().ConfigureAwait(false);
        }

        reader?.Dispose();

        await Stream.DisposeAsync().ConfigureAwait(false);
    }
}
