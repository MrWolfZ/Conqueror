namespace Conqueror.Transport.FileSystem;

internal sealed class SeqIndexFile(DirectoryPath baseDirectoryPath) : IDisposable
{
    private const int EntryLength = EntryId.IdLength + 1;

    private readonly DisposableSemaphore semaphore = new();
    private readonly FilePath seqFilePath = baseDirectoryPath.File("seq-index.txt");

    public async Task<SeqNr> Append(EntryId id, CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        using var d = await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var handle = await seqFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        _ = handle.Stream.Seek(0, SeekOrigin.End);

        var currentSeqNr = handle.Stream.Length / EntryLength;

        // we do not allow cancellation here to prevent corruption of the file
        await handle.Writer.WriteAsync($"{id}\n".AsMemory(), CancellationToken.None).ConfigureAwait(false);

        return new((ulong)currentSeqNr + 1);
    }

    public async IAsyncEnumerable<(EntryId EntryId, SeqNr SeqNr)> ReadChanges(
        SeqNr startFromSeqNr,
        TimeSpan pollingInterval,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        var currentSeqNr = (ulong)startFromSeqNr;

        while (!cancellationToken.IsCancellationRequested)
        {
            var handle = await seqFilePath.OpenRead(cancellationToken).ConfigureAwait(false);

            EntryId[]? entries = null;

            await using (handle.ConfigureAwait(false))
            {
                var startAt = currentSeqNr * EntryLength;
                var nrOfCharsToRead = (int)((ulong)handle.Stream.Length - startAt);
                var nrOfEntries = nrOfCharsToRead / EntryLength;

                if (nrOfCharsToRead > 0)
                {
                    using var buffer = new CharBuffer(nrOfCharsToRead);

                    var nowAt = handle.Stream.Seek((long)startAt, SeekOrigin.Begin);

                    Debug.Assert(nowAt == (long)startAt, $"expected to seek to {startAt}, but seeked to {nowAt}");

                    var readCount = await handle.Reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

                    Debug.Assert(readCount == nrOfCharsToRead, $"expected to read {nrOfCharsToRead} bytes, but read {readCount}");

                    entries = new EntryId[nrOfEntries];

                    for (int entryIndex = 0, bufferIndex = 0; bufferIndex < nrOfEntries * EntryLength; bufferIndex += EntryLength, entryIndex += 1)
                    {
                        entries[entryIndex] = new(new(buffer.Span.Slice(bufferIndex, EntryId.IdLength)));
                    }
                }
            }

            if (entries is not null)
            {
                foreach (var entry in entries)
                {
                    currentSeqNr += 1;
                    yield return (entry, new(currentSeqNr));
                }
            }

            await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private static void ThrowOnInvalidLength(ReadWriteFileHandle handle)
    {
        if (handle.Stream.Length % EntryLength != 0)
        {
            throw new InvalidOperationException(
                $"signal seq index file '{handle.FilePath}' is corrupted, expected length to be a multiple of {EntryLength}, but it was {handle.Stream.Length}");
        }
    }
}
