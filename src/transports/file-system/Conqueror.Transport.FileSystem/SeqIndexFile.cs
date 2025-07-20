using System.Threading.Channels;

namespace Conqueror.Transport.FileSystem;

[SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "for performance")]
internal sealed class SeqIndexFile(DirectoryPath baseDirectoryPath, TagIdFiles tagIdFiles) : IDisposable
{
    private const int SeqNrLength = 12;
    private const int HeaderLength = SeqNrLength + 1; // including newline
    private const int TagIdLength = 6;
    private const int EntryLength = EntryId.IdLength + 1 + TagIdLength + 1; // including separator and newline
    private const char Separator = '|';

    private readonly ConcurrentDictionary<TimeSpan, Poller> pollerByPollingInterval = new();
    private readonly FilePath seqFilePath = baseDirectoryPath.File("seq-index.txt");

    public SeqNr Append(EntryId id, Tag tag, CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        var tagId = tagIdFiles.GetId(tag, cancellationToken);

        using var handle = seqFilePath.OpenReadWrite(cancellationToken);

        ThrowOnInvalidLength(handle);

        SeqNr compactedSeqNr;

        if (handle.Stream.Length == 0)
        {
            compactedSeqNr = new(0);
            WriteCompactedSeqNr(handle.Writer, compactedSeqNr);
        }
        else
        {
            compactedSeqNr = GetCompactedSeqNr(handle.Reader);
        }

        var nrOfEntries = (ulong)(handle.Stream.Length - HeaderLength) / EntryLength;

        var currentSeqNr = new SeqNr(compactedSeqNr + nrOfEntries + 1);

        _ = handle.Stream.Seek(0, SeekOrigin.End);

        var content = $"{id}{Separator}{tagId.ToPaddedString(TagIdLength)}\n";

        Debug.Assert(content.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {content.Length}");

        // we do not allow cancellation here to prevent corruption of the file
        handle.Writer.Write(content.AsMemory());

        foreach (var poller in pollerByPollingInterval.Values)
        {
            poller.Notify(compactedSeqNr, currentSeqNr);
        }

        return currentSeqNr;
    }

    public async IAsyncEnumerable<IReadOnlyCollection<(EntryId EntryId, Tag Tag, SeqNr SeqNr)>> ReadChanges(
        SeqNr startFromSeqNr,
        TimeSpan pollingInterval,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        var currentSeqNr = startFromSeqNr;

        var poller = pollerByPollingInterval.GetOrAdd(pollingInterval, i => new(seqFilePath, i));

        var channel = Channel.CreateBounded<(SeqNr CompactedSeqNr, SeqNr LatestSeqNr)>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

        using var watchDisposable = poller.Watch(channel.Writer);

        await foreach (var (compactedSeqNr, latestSeqNr) in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (latestSeqNr <= currentSeqNr)
            {
                continue;
            }

            foreach (var batch in ReadBetween(
                         currentSeqNr,
                         latestSeqNr,
                         compactedSeqNr,
                         cancellationToken))
            {
                yield return batch;
            }

            currentSeqNr = latestSeqNr;
        }
    }

    public void Dispose()
    {
        foreach (var poller in pollerByPollingInterval.Values)
        {
            poller.Dispose();
        }
    }

    private IEnumerable<IReadOnlyCollection<(EntryId EntryId, Tag Tag, SeqNr SeqNr)>> ReadBetween(
        SeqNr startAt,
        SeqNr endAt,
        SeqNr compactedSeqNr,
        CancellationToken cancellationToken)
    {
        var handle = seqFilePath.OpenRead(cancellationToken);

        Debug.Assert(handle is not null, "expected handle to be non-null");

        (EntryId EntryId, Tag Tag, SeqNr SeqNr)[] entries;

        using (handle)
        {
            var seqNrOffset = startAt - compactedSeqNr;

            // ReSharper disable once ArrangeRedundantParentheses
            var startAtOffset = (seqNrOffset * EntryLength) + HeaderLength;

            // TODO: read in batches from read-through cache
            var nrOfEntries = (int)(endAt - startAt);
            var nrOfCharsToRead = nrOfEntries * EntryLength;

            Debug.Assert(nrOfCharsToRead > 0, $"expected nr of chars to read to be greater than 0, but was {nrOfCharsToRead}");

            var buffer = new CharBuffer(nrOfCharsToRead);

            var nowAt = handle.Stream.Seek((long)startAtOffset, SeekOrigin.Begin);
            handle.Reader.DiscardBufferedData();

            Debug.Assert(nowAt == (long)startAtOffset, $"expected to seek to {startAtOffset}, but seeked to {nowAt}");

            var readCount = handle.Reader.Read(buffer.Span);

            Debug.Assert(readCount == nrOfCharsToRead, $"expected to read {nrOfCharsToRead} bytes, but read {readCount}");

            entries = new (EntryId EntryId, Tag Tag, SeqNr SeqNr)[nrOfEntries];

            for (int entryIndex = 0, bufferIndex = 0; bufferIndex < nrOfEntries * EntryLength; bufferIndex += EntryLength, entryIndex += 1)
            {
                var entryId = new EntryId(new(buffer.Span.Slice(bufferIndex, EntryId.IdLength)));
                var tagId = new TagId(uint.Parse(buffer.Span.Slice(bufferIndex + EntryId.IdLength + 1, TagIdLength)));
                var tag = tagIdFiles.GetById(tagId, cancellationToken);
                var seqNr = new SeqNr(startAt + (ulong)entryIndex + 1);
                entries[entryIndex] = (entryId, tag, seqNr);
            }
        }

        yield return entries;
    }

    [SuppressMessage(
        "Minor Code Smell",
        "S3398:\"private\" methods called only by inner classes should be moved to those classes",
        Justification = "the poller should be simple and only contain code related to polling, not file access")]
    private static (SeqNr CompactedSeqNr, SeqNr LatestSeqNr)? GetCurrentSeqNr(FilePath seqFilePath, CancellationToken cancellationToken)
    {
        using var handle = seqFilePath.OpenRead(cancellationToken);

        if (handle is null)
        {
            return null;
        }

        if (handle.Stream.Length < HeaderLength)
        {
            return null;
        }

        var compactedSeqNr = GetCompactedSeqNr(handle.Reader);
        var nrOfEntries = (handle.Stream.Length - HeaderLength) / EntryLength;

        return (compactedSeqNr, new(compactedSeqNr + (ulong)nrOfEntries));
    }

    private static SeqNr GetCompactedSeqNr(StreamReader reader)
    {
        Span<char> buffer = stackalloc char[SeqNrLength];
        var readChars = reader.Read(buffer);

        Debug.Assert(readChars == SeqNrLength, $"expected to read {SeqNrLength} chars but read {readChars}");

        return new(ulong.Parse(buffer));
    }

    private static void WriteCompactedSeqNr(StreamWriter writer, SeqNr seqNr)
    {
        _ = writer.BaseStream.Seek(0, SeekOrigin.Begin);

        writer.Write($"{seqNr.ToPaddedString(SeqNrLength)}\n".AsMemory());
        writer.Flush();
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

    private sealed class Poller(FilePath seqFilePath, TimeSpan pollingInterval) : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly ConcurrentDictionary<Guid, ChannelWriter<(SeqNr CompactedSeqNr, SeqNr LatestSeqNr)>> channelWriters = [];

        private long latestFileLength = -1;
        private (SeqNr CompactedSeqNr, SeqNr LatestSeqNr)? latestResult;
        private Timer? timer;
        private int watchCount;

        public IDisposable Watch(ChannelWriter<(SeqNr CompactedSeqNr, SeqNr LatestSeqNr)> channelWriter)
        {
            var channelWriterId = Guid.NewGuid();
            _ = channelWriters.TryAdd(channelWriterId, channelWriter);

            if (latestResult is not null)
            {
                _ = channelWriter.TryWrite(latestResult.Value);
            }

            if (Interlocked.Increment(ref watchCount) == 1)
            {
                timer = new(
                    OnTimerElapsed,
                    null,
                    TimeSpan.Zero,
                    pollingInterval);
            }

            return new Disposable(this, channelWriterId);
        }

        public void Notify(SeqNr compactedSeqNr, SeqNr latestSeqNr)
        {
            var res = (compactedSeqNr, latestSeqNr);

            foreach (var channelWriter in channelWriters.Values)
            {
                _ = channelWriter.TryWrite(res);
            }

            latestResult = res;
        }

        public void Dispose()
        {
            timer?.Dispose();

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        private void OnTimerElapsed(object? state)
        {
            try
            {
                var seqFileInfo = seqFilePath.GetFileInfo();

                if (!seqFileInfo.Exists || seqFileInfo.Length == latestFileLength)
                {
                    return;
                }

                if (GetCurrentSeqNr(seqFilePath, cancellationTokenSource.Token) is { } seqNr)
                {
                    foreach (var channelWriter in channelWriters.Values)
                    {
                        _ = channelWriter.TryWrite(seqNr);
                    }

                    latestResult = seqNr;
                }

                latestFileLength = seqFileInfo.Length;
            }
            catch (Exception e)
            {
                foreach (var channelWriter in channelWriters.Values)
                {
                    _ = channelWriter.TryComplete(e);
                }
            }
        }

        private sealed class Disposable(Poller poller, Guid channelWriterId) : IDisposable
        {
            public void Dispose()
            {
                _ = poller.channelWriters.Remove(channelWriterId, out _);

                if (Interlocked.Decrement(ref poller.watchCount) == 0)
                {
                    poller.timer?.Dispose();
                    poller.timer = null;

                    poller.latestResult = null;
                    poller.latestFileLength = -1;
                }
            }
        }
    }
}
