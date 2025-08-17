namespace Conqueror.Transport.FileSystem;

using System.Globalization;
using System.Text.Json;

internal static class FileOperations
{
    [ThreadStatic]
    private static Random? Random;

    public static FileInfo GetFileInfo(this FilePath filePath) => new(filePath);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    public static ReadOnlyFileHandle? OpenRead(this FilePath filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileStream = filePath.OpenWithRetry(FileAccess.Read, cancellationToken, FileMode.Open);

            return new ReadOnlyFileHandle(filePath, fileStream);
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

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    public static ReadWriteFileHandle OpenReadWrite(this FilePath filePath, CancellationToken cancellationToken)
    {
        var fileStream = filePath.OpenWithRetry(FileAccess.ReadWrite, cancellationToken);

        return new ReadWriteFileHandle(filePath, fileStream);
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, stream is passed to caller in the handle"
    )]
    public static ReadOnlyFileHandle? TryOpenRead(this FilePath filePath)
    {
        try
        {
            var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);

            return new ReadOnlyFileHandle(filePath, fileStream);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void WriteJson<T>(this ReadWriteFileHandle handle, T value, JsonTypeInfo<T> jsonTypeInfo) =>
        JsonSerializer.Serialize(handle.Stream, value, jsonTypeInfo);

    public static T ReadJson<T>(this ReadWriteFileHandle handle, JsonTypeInfo<T> jsonTypeInfo)
    {
        var result = JsonSerializer.Deserialize(handle.Stream, jsonTypeInfo);

        return result
            ?? throw new IOException(
                $"failed to JSON-deserialize file '{handle.FilePath}' to object of type '{typeof(T)}'"
            );
    }

    public static void DeleteLineFromFile(ReadWriteFileHandle handle, int lineNumber, int lineLength)
    {
        if (lineNumber < 0)
        {
            throw new ArgumentException(
                string.Create(CultureInfo.InvariantCulture, $"Invalid line number: {lineNumber}"),
                nameof(lineNumber)
            );
        }

        if (lineLength <= 0)
        {
            throw new ArgumentException(
                string.Create(CultureInfo.InvariantCulture, $"Invalid line length: {lineLength}"),
                nameof(lineLength)
            );
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
            if (bytesRead is 0)
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

    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "we don't need security here"
    )]
    [SuppressMessage(
        "Major Bug",
        "S1751:Loops with at most one iteration should be refactored",
        Justification = "by design"
    )]
    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "we want this to be sync"
    )]
    private static FileStream OpenWithRetry(
        this FilePath filePath,
        FileAccess access,
        CancellationToken cancellationToken,
        FileMode mode = FileMode.OpenOrCreate,
        FileShare share = FileShare.Read,
        int maxAttempts = 25,
        int initialDelayMs = 0,
        int maxDelayMs = 1000
    )
    {
        var attempt = 0;
        var delayMs = initialDelayMs;

        Stopwatch? sw = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new FileStream(filePath, mode, access, share, bufferSize: 128);
            }
            catch (IOException iex) when (iex is not FileNotFoundException and not DirectoryNotFoundException)
            {
                sw ??= Stopwatch.StartNew();
                Random ??= new Random();

                attempt += 1;

                if (attempt >= maxAttempts)
                {
                    throw new IOException(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Could not acquire lock on file '{filePath}' after {maxAttempts} attempts (elapsed time: {sw.ElapsedMilliseconds}ms)."
                        ),
                        iex
                    );
                }

                if (attempt >= 10)
                {
                    // Exponential backoff with jitter
                    var baseDelay = delayMs is 0 ? 10 : Math.Min(delayMs * 2, maxDelayMs);

                    // Add ±50% jitter
                    var jitterRange = (int)(baseDelay * 0.5);
                    delayMs = baseDelay + Random.Next(-jitterRange, jitterRange + 1);
                    delayMs = Math.Max(val1: 1, delayMs);
                }
                else if (attempt >= 5)
                {
                    delayMs = Random.Next(minValue: 1, maxValue: 5);
                }
                else
                {
                    // on the first we attempt, we don't want to increase the delay
                }

                if (delayMs > 0)
                {
                    Thread.Sleep(delayMs);
                }
                else
                {
                    Thread.SpinWait(iterations: 1);
                }
            }
        }
    }
}

internal class ReadOnlyFileHandle(FilePath path, FileStream stream) : IDisposable
{
    private StreamReader? reader;

    public FilePath FilePath => path;

    public FileStream Stream => stream;

    public StreamReader Reader => reader ??= new StreamReader(Stream, leaveOpen: true);

    public void Dispose()
    {
        Dispose(isDisposing: true);
        GC.SuppressFinalize(this);
    }

    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "we want this to be sync"
    )]
    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            reader?.Dispose();
            stream.Dispose();
        }
    }
}

internal sealed class ReadWriteFileHandle(FilePath path, FileStream stream) : ReadOnlyFileHandle(path, stream)
{
    private StreamWriter? writer;

    public StreamWriter Writer => writer ??= new StreamWriter(Stream, leaveOpen: true);

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            writer?.Dispose();
        }

        base.Dispose(isDisposing);
    }
}
