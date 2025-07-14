namespace Conqueror.Transport.FileSystem;

internal sealed class SeqIndexFile(DirectoryPath baseDirectoryPath) : IDisposable
{
    private const int SeqNrLength = 12;
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

        SeqNr currentSeqNr;

        if (handle.Stream.Length == 0)
        {
            currentSeqNr = new(0);
            await WriteCompactedSeqNr(handle.Writer, currentSeqNr).ConfigureAwait(false);
        }
        else
        {
            currentSeqNr = await GetCompactedSeqNr(handle.Reader, cancellationToken).ConfigureAwait(false);
        }

        var nrOfEntries = (ulong)(handle.Stream.Length - SeqNrLength - 1) / EntryLength;

        currentSeqNr = new(currentSeqNr + nrOfEntries + 1);

        _ = handle.Stream.Seek(0, SeekOrigin.End);

        // we do not allow cancellation here to prevent corruption of the file
        await handle.Writer.WriteAsync($"{id}\n".AsMemory(), CancellationToken.None).ConfigureAwait(false);

        return currentSeqNr;
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
                if (handle.Stream.Length >= SeqNrLength + 1)
                {
                    var compactedSeqNr = await GetCompactedSeqNr(handle.Reader, cancellationToken).ConfigureAwait(false);
                    var seqNrOffset = currentSeqNr - compactedSeqNr;

                    // ReSharper disable once ArrangeRedundantParentheses
                    var startAt = (seqNrOffset * EntryLength) + SeqNrLength + 1;

                    var nrOfCharsToRead = (int)((ulong)handle.Stream.Length - startAt);
                    var nrOfEntries = nrOfCharsToRead / EntryLength;

                    if (nrOfCharsToRead > 0)
                    {
                        using var buffer = new CharBuffer(nrOfCharsToRead);

                        var nowAt = handle.Stream.Seek((long)startAt, SeekOrigin.Begin);
                        handle.Reader.DiscardBufferedData();

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
            }

            if (entries is not null)
            {
                foreach (var entry in entries)
                {
                    currentSeqNr += 1;

                    yield return (entry, new(currentSeqNr));
                }

                continue; // try eagerly reading the next batch of entries instead of waiting
            }

            await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private static async Task<SeqNr> GetCompactedSeqNr(StreamReader reader, CancellationToken cancellationToken)
    {
        using var buffer = new CharBuffer(SeqNrLength);
        var readChars = await reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

        Debug.Assert(readChars == SeqNrLength, $"expected to read {SeqNrLength} chars but read {readChars}");

        return new(ulong.Parse(buffer.Span));
    }

    private static async Task WriteCompactedSeqNr(StreamWriter writer, SeqNr seqNr)
    {
        _ = writer.BaseStream.Seek(0, SeekOrigin.Begin);

        await writer.WriteAsync($"{seqNr.ToPaddedString(SeqNrLength)}\n".AsMemory(), CancellationToken.None).ConfigureAwait(false);
        await writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static void ThrowOnInvalidLength(ReadWriteFileHandle handle)
    {
        if (handle.Stream.Length == 0)
        {
            return;
        }

        if (handle.Stream.Length < SeqNrLength + 1)
        {
            throw new InvalidOperationException(
                $"signal seq index file '{handle.FilePath}' is corrupted, expected it to be prefixed with {SeqNrLength + 1} chars, but it was {handle.Stream.Length} chars long");
        }

        var entriesLength = handle.Stream.Length - SeqNrLength - 1;

        if (entriesLength % EntryLength != 0)
        {
            throw new InvalidOperationException(
                $"signal seq index file '{handle.FilePath}' is corrupted, expected length to be a multiple of {EntryLength}, but it was {entriesLength}");
        }
    }
}
