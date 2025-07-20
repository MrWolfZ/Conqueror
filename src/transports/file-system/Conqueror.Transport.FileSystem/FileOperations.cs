using System.Text.Json.Serialization.Metadata;

namespace Conqueror.Transport.FileSystem;

internal static class FileOperations
{
    [ThreadStatic]
    private static Random? random;

    public static FileInfo GetFileInfo(this FilePath filePath) => new(filePath);

    public static async ValueTask<ReadOnlyFileHandle?> OpenRead(this FilePath filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileStream = await filePath.OpenWithRetry(FileAccess.Read, cancellationToken, mode: FileMode.Open).ConfigureAwait(false);

            return new(filePath, fileStream);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public static async ValueTask<ReadWriteFileHandle> OpenReadWrite(this FilePath filePath, CancellationToken cancellationToken)
    {
        var fileStream = await filePath.OpenWithRetry(FileAccess.ReadWrite, cancellationToken).ConfigureAwait(false);

        return new(filePath, fileStream);
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, stream is passed to caller in the handle")]
    public static ReadOnlyFileHandle? TryOpenRead(this FilePath filePath)
    {
        try
        {
            var fileStream = new FileStream(
                filePath,
                FileMode.OpenOrCreate,
                FileAccess.Read,
                FileShare.None);

            return new(filePath, fileStream);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void WriteJson<T>(this ReadWriteFileHandle handle, T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        JsonSerializer.Serialize(handle.Stream, value, jsonTypeInfo);
    }

    public static T ReadJson<T>(this ReadWriteFileHandle handle, JsonTypeInfo<T> jsonTypeInfo)
    {
        var result = JsonSerializer.Deserialize(handle.Stream, jsonTypeInfo);

        return result ?? throw new IOException($"failed to JSON-deserialize file '{handle.FilePath}' to object of type '{typeof(T)}'");
    }

    public static void DeleteLineFromFile(ReadWriteFileHandle handle, int lineNumber, int lineLength)
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

        Span<byte> buffer = stackalloc byte[lineLength];

        // Shift all lines after the deleted one forward
        while (readPos < stream.Length)
        {
            _ = stream.Seek(readPos, SeekOrigin.Begin);
            var bytesRead = stream.Read(buffer);
            if (bytesRead == 0)
            {
                break;
            }

            _ = stream.Seek(writePos, SeekOrigin.Begin);
            stream.Write(buffer);

            readPos += lineLength;
            writePos += lineLength;
        }

        // Truncate the file to remove the last line
        stream.SetLength(stream.Length - lineLength);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "we don't need security here")]
    [SuppressMessage("Major Bug", "S1751:Loops with at most one iteration should be refactored", Justification = "by design")]
    private static async ValueTask<FileStream> OpenWithRetry(
        this FilePath filePath,
        FileAccess access,
        CancellationToken cancellationToken,
        FileMode mode = FileMode.OpenOrCreate,
        FileShare share = FileShare.Read,
        int maxAttempts = 25,
        int initialDelayMs = 0,
        int maxDelayMs = 1000)
    {
        var attempt = 0;
        var delayMs = initialDelayMs;

        Stopwatch? sw = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new(
                    filePath,
                    mode,
                    access,
                    share,
                    bufferSize: 128);
            }
            catch (IOException iex) when (iex is not FileNotFoundException and not DirectoryNotFoundException)
            {
                sw ??= Stopwatch.StartNew();
                random ??= new();

                attempt += 1;

                if (attempt >= maxAttempts)
                {
                    throw new IOException(
                        $"Could not acquire lock on file '{filePath}' after {maxAttempts} attempts (elapsed time: {sw.ElapsedMilliseconds}ms).",
                        iex);
                }

                if (attempt >= 10)
                {
                    // Exponential backoff with jitter
                    var baseDelay = delayMs == 0 ? 10 : Math.Min(delayMs * 2, maxDelayMs);

                    // Add ±50% jitter
                    var jitterRange = (int)(baseDelay * 0.5);
                    delayMs = baseDelay + random.Next(-jitterRange, jitterRange + 1);
                    delayMs = Math.Max(1, delayMs);
                }
                else if (attempt >= 5)
                {
                    delayMs = random.Next(1, 5);
                }

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Yield();
                }
            }
        }
    }
}

internal sealed class ReadOnlyFileHandle(FilePath path, FileStream stream) : IDisposable
{
    private StreamReader? reader;

    public FilePath FilePath => path;

    public FileStream Stream => stream;

    public StreamReader Reader => reader ??= new(Stream, leaveOpen: true);

    public void Dispose()
    {
        reader?.Dispose();
        stream.Dispose();
    }
}

internal sealed class ReadWriteFileHandle(FilePath path, FileStream stream) : IDisposable
{
    private StreamReader? reader;
    private StreamWriter? writer;

    public FilePath FilePath => path;

    public FileStream Stream => stream;

    public StreamReader Reader => reader ??= new(Stream, leaveOpen: true);

    public StreamWriter Writer => writer ??= new(Stream, leaveOpen: true);

    public void Dispose()
    {
        reader?.Dispose();
        writer?.Dispose();
        stream.Dispose();
    }
}
