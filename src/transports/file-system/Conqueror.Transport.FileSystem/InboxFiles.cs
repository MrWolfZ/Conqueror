namespace Conqueror.Transport.FileSystem;

internal sealed class InboxFiles(DirectoryPath baseDirectoryPath) : IDisposable
{
    private const int SeqNrLength = 12; // up to 1 trillion messages
    private const int SeparatorLength = 1;
    private const int StatusLength = 1;
    private const int TimestampLength = 24; // ISO timestamp, e.g. 2025-01-01T12:12:12.123Z

    private const string EmptyTimestamp = "0000-00-00T00:00:00.000Z";

    private const int EntryLength = SeqNrLength
                                    + SeparatorLength
                                    + EntryId.IdLength
                                    + SeparatorLength
                                    + StatusLength
                                    + SeparatorLength
                                    + TimestampLength
                                    + 1; // 1 for the newline

    private const char Separator = '|';
    private const char StateMarker = 'M';
    private const char StateAvailable = 'A';
    private const char StateLeased = 'L';

    private readonly ConcurrentDictionary<Tag, DisposableSemaphore> semaphoreByTag = [];

    public async Task Append(
        Tag tag,
        SeqNr seqNr,
        EntryId id,
        CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        var inboxFilePath = GetInboxFilePath(tag);

        inboxFilePath.DirectoryPath.EnsureExists();

        using var d = await GetSemaphoreForTag(tag).WaitAsync(cancellationToken).ConfigureAwait(false);

        var handle = await inboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        // if we just created the file, we need to create the initial marker entry
        if (handle.Stream.Length == 0)
        {
            var emptyId = new EntryId(new('_', EntryId.IdLength));
            var markerEntry = $"{seqNr.ToPaddedString(SeqNrLength)}{Separator}{emptyId}{Separator}{StateMarker}{Separator}{EmptyTimestamp}\n";

            Debug.Assert(markerEntry.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {markerEntry.Length}");

            // we do not allow cancellation here to prevent corruption of the file
            await handle.Writer.WriteAsync(markerEntry.AsMemory(), CancellationToken.None).ConfigureAwait(false);
            await handle.Writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            // otherwise we override the marker with the new seq nr (if it is not a duplicate)
            var buffer = new char[SeqNrLength];
            var readChars = await handle.Reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            Debug.Assert(readChars == SeqNrLength, $"expected to read {SeqNrLength} chars, but got {readChars}");

            var prevSeqNr = ulong.Parse(buffer);

            // if for some reason we are trying to append an older entry, we just skip it
            if (seqNr <= prevSeqNr)
            {
                return;
            }

            var seekResult = handle.Stream.Seek(0, SeekOrigin.Begin);

            Debug.Assert(seekResult == 0, $"expected to seek to start of file, but got {seekResult}");

            // we do not allow cancellation here to prevent corruption of the file
            await handle.Writer.WriteAsync(seqNr.ToPaddedString(SeqNrLength).AsMemory(), CancellationToken.None).ConfigureAwait(false);
            await handle.Writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        _ = handle.Stream.Seek(0, SeekOrigin.End);

        var entry = $"{seqNr.ToPaddedString(SeqNrLength)}{Separator}{id}{Separator}{StateAvailable}{Separator}{EmptyTimestamp}\n";

        Debug.Assert(entry.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {entry.Length}");

        // we do not allow cancellation here to prevent corruption of the file
        await handle.Writer.WriteAsync(entry.AsMemory(), CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<SeqNr> GetCurrentSeqNr(
        Tag tag,
        CancellationToken cancellationToken)
    {
        var inboxFilePath = GetInboxFilePath(tag);

        if (!inboxFilePath.FileExists())
        {
            return new(0);
        }

        var handle = await inboxFilePath.OpenRead(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        using var buffer = new CharBuffer(SeqNrLength);

        var readChars = await handle.Reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

        Debug.Assert(readChars == SeqNrLength, $"expected to read {EntryLength} chars but got {readChars}");

        return new(ulong.Parse(buffer.Span));
    }

    public async IAsyncEnumerable<(Tag Tag, SeqNr SeqNr, EntryId Id)> LeaseNextMessage(
        IReadOnlyCollection<Tag> tags,
        TimeSpan pollingInterval,
        TimeSpan leaseDuration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Debug.Assert(tags.Count > 0, "expected at least one tag, but there were none");

        // always acquire semaphores (and file locks) in alphabetical order to prevent deadlocks
        var sortedTags = tags.OrderBy(static t => t, StringComparer.OrdinalIgnoreCase).ToArray();
        var tagInboxFiles = sortedTags.Select(GetInboxFilePath).ToArray();

        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var leaseExpiresAt = now + leaseDuration;

            (Tag Tag, SeqNr SeqNr, EntryId Id)? nextEntryToYield = null;

            var semaphoreDisposable = new AggregateDisposable(sortedTags.Length);
            var inboxHandleDisposable = new AggregateAsyncDisposable(tagInboxFiles.Length);
            var inboxHandles = new ReadWriteFileHandle?[tagInboxFiles.Length];

            using (semaphoreDisposable)
            {
                await using (inboxHandleDisposable)
                {
                    for (var i = 0; i < sortedTags.Length; i += 1)
                    {
                        var tag = sortedTags[i];
                        var inboxFilePath = tagInboxFiles[i];

                        semaphoreDisposable.Add(await GetSemaphoreForTag(tag).WaitAsync(cancellationToken).ConfigureAwait(false));

                        cancellationToken.ThrowIfCancellationRequested();

                        // we only acquire the file lock when the file exists and has at least one entry (ignoring the marker entry)
                        if (inboxFilePath.FileExists() && inboxFilePath.GetLength() > EntryLength)
                        {
                            var handle = await inboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);
                            inboxHandleDisposable.Add(handle);

                            inboxHandles[i] = handle;
                        }
                    }

                    var nextAvailableEntry = await FindNextAvailableEntry(
                            inboxHandles,
                            now,
                            0,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (nextAvailableEntry is { Entry: var entry, TagIdx: var tagIdx, EntryLineNrInFile: var entryLineNrInFile })
                    {
                        var tag = sortedTags[tagIdx];
                        var handle = inboxHandles[tagIdx];
                        Debug.Assert(handle is not null, "expected the inbox file handle to be non-null");

                        await UpdateInboxEntry(
                                handle,
                                entryLineNrInFile,
                                entry with { State = StateLeased, LeaseExpiresAt = leaseExpiresAt })
                            .ConfigureAwait(false);

                        nextEntryToYield = (tag, entry.SeqNr, entry.Id);
                    }
                }
            }

            if (nextEntryToYield is not null)
            {
                yield return nextEntryToYield.Value;

                continue; // try eagerly leasing the next message instead of waiting
            }

            await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);
        }

        static async Task<(Entry Entry, int TagIdx, int EntryLineNrInFile)?> FindNextAvailableEntry(
            ReadWriteFileHandle?[] tagInboxFiles,
            DateTime now,
            int tagIdx,
            CancellationToken cancellationToken)
        {
            if (tagIdx >= tagInboxFiles.Length)
            {
                return null;
            }

            var nextAvailableEntry = await FindNextAvailableEntry(
                    tagInboxFiles,
                    now,
                    tagIdx + 1,
                    cancellationToken)
                .ConfigureAwait(false);

            var inboxFileHandle = tagInboxFiles[tagIdx];

            if (inboxFileHandle is not null)
            {
                ThrowOnInvalidLength(inboxFileHandle);

                // skip marker entry
                _ = inboxFileHandle.Stream.Seek(EntryLength, SeekOrigin.Begin);

                using var buffer = new CharBuffer(EntryLength);
                var entryLineNrInFile = 1;

                while (!inboxFileHandle.Reader.EndOfStream)
                {
                    var readBytes = await inboxFileHandle.Reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

                    Debug.Assert(readBytes == EntryLength, $"expected to read {EntryLength} bytes, but read {readBytes}");

                    var entry = Entry.Parse(buffer.Span);
                    var entryIsAvailable = entry.State == StateAvailable || now >= entry.LeaseExpiresAt;
                    var entryHasLowerSeqNr = nextAvailableEntry is null || entry.SeqNr < nextAvailableEntry.Value.Entry.SeqNr;

                    if (entryIsAvailable && entryHasLowerSeqNr)
                    {
                        nextAvailableEntry = (entry, tagIdx, entryLineNrInFile);
                    }

                    entryLineNrInFile += 1;
                }
            }

            return nextAvailableEntry;
        }
    }

    public async Task GiveUpLease(Tag tag, SeqNr seqNr, CancellationToken cancellationToken)
    {
        using var d = await GetSemaphoreForTag(tag).WaitAsync(cancellationToken).ConfigureAwait(false);

        var tagInboxFilePath = GetInboxFilePath(tag);

        tagInboxFilePath.DirectoryPath.AssertExists();

        var handle = await tagInboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        // skip marker entry
        _ = handle.Stream.Seek(EntryLength, SeekOrigin.Begin);

        using var buffer = new CharBuffer(EntryLength);
        var entryLineNrInFile = 1;

        while (!handle.Reader.EndOfStream)
        {
            var readBytes = await handle.Reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

            Debug.Assert(readBytes == EntryLength, $"expected to read {EntryLength} bytes, but read {readBytes}");

            var entry = Entry.Parse(buffer.Span);

            if (entry.SeqNr == seqNr)
            {
                await UpdateInboxEntry(
                        handle,
                        entryLineNrInFile,
                        entry with { State = StateAvailable, LeaseExpiresAt = null })
                    .ConfigureAwait(false);

                return;
            }

            entryLineNrInFile += 1;
        }
    }

    public async Task RemoveEntry(
        Tag tag,
        SeqNr seqNr,
        CancellationToken cancellationToken)
    {
        using var d = await GetSemaphoreForTag(tag).WaitAsync(cancellationToken).ConfigureAwait(false);

        var tagInboxFilePath = GetInboxFilePath(tag);

        tagInboxFilePath.DirectoryPath.AssertExists();

        var handle = await tagInboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        // skip marker entry
        _ = handle.Stream.Seek(EntryLength, SeekOrigin.Begin);

        using var buffer = new CharBuffer(EntryLength);
        var entryLineNrInFile = 1;

        while (!handle.Reader.EndOfStream)
        {
            var readBytes = await handle.Reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

            Debug.Assert(readBytes == EntryLength, $"expected to read {EntryLength} bytes, but read {readBytes}");

            var entry = Entry.Parse(buffer.Span);

            if (entry.SeqNr == seqNr)
            {
                await FileOperations.DeleteLineFromFile(
                                        handle,
                                        entryLineNrInFile,
                                        EntryLength,
                                        CancellationToken.None)
                                    .ConfigureAwait(false);

                return;
            }

            entryLineNrInFile += 1;
        }
    }

    public void Dispose()
    {
        foreach (var semaphore in semaphoreByTag.Values)
        {
            semaphore.Dispose();
        }
    }

    private DisposableSemaphore GetSemaphoreForTag(Tag tag)
    {
        if (!semaphoreByTag.TryGetValue(tag, out var semaphore))
        {
            var newSemaphore = new DisposableSemaphore();

            if (!semaphoreByTag.TryAdd(tag, newSemaphore))
            {
                newSemaphore.Dispose();
            }

            semaphore = semaphoreByTag[tag];
        }

        return semaphore;
    }

    private FilePath GetInboxFilePath(Tag tag) => baseDirectoryPath.SubDir(tag).File(".inbox.txt");

    private static async Task UpdateInboxEntry(ReadWriteFileHandle inboxFileHandle, int entryIdx, Entry entry)
    {
        var seekTo = entryIdx * EntryLength;
        var res = inboxFileHandle.Stream.Seek(seekTo, SeekOrigin.Begin);

        Debug.Assert(res == seekTo, $"expected to seek to {seekTo}, but seeked to {res}");

        var leaseExpiresAt = entry.LeaseExpiresAt is not null
            ? entry.LeaseExpiresAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
            : EmptyTimestamp;

        var lineContent = $"{entry.SeqNr.ToPaddedString(SeqNrLength)}{Separator}{entry.Id}{Separator}{entry.State}{Separator}{leaseExpiresAt}\n";

        // we do not allow cancellation here to prevent corruption of the file
        await inboxFileHandle.Writer.WriteAsync(lineContent.AsMemory(), CancellationToken.None).ConfigureAwait(false);
    }

    private static void ThrowOnInvalidLength(ReadWriteFileHandle handle)
    {
        if (handle.Stream.Length % EntryLength != 0)
        {
            var buffer = new char[1024];
            var readBytes = handle.Reader.Read(buffer);

            var content = new string(buffer, 0, readBytes);

            throw new InvalidOperationException(
                $"message queue inbox file '{handle.FilePath}' is corrupted, expected length to be a multiple of {EntryLength}, but it was {handle.Stream.Length} with content:\n{content}");
        }
    }

    private readonly record struct Entry(
        SeqNr SeqNr,
        EntryId Id,
        char State,
        DateTime? LeaseExpiresAt)
    {
        public static Entry Parse(ReadOnlySpan<char> span)
        {
            var index = 0;

            var seqNr = ulong.Parse(span.Slice(index, SeqNrLength));
            index += SeqNrLength;

            Debug.Assert(span[index] == Separator, $"expected separator '{Separator}' at index {index}, but found '{span[index]}' in string {span.ToString()}");
            index += 1;

            var id = span.Slice(index, EntryId.IdLength);
            index += EntryId.IdLength;

            Debug.Assert(span[index] == Separator, $"expected separator '{Separator}' at index {index}, but found '{span[index]}' in string {span.ToString()}");
            index += 1;

            var state = span[index];
            index += 1;

            Debug.Assert(span[index] == Separator, $"expected separator '{Separator}' at index {index}, but found '{span[index]}' in string {span.ToString()}");
            index += 1;

            var leaseExpiresAt = span.Slice(index, TimestampLength);
            index += TimestampLength;

            Debug.Assert(span[index] == '\n', $"expected newline, but found '{span[index]}' at index {index}, in string {span.ToString()}");

            return new(
                SeqNr: new(seqNr),
                Id: new(new(id)),
                State: state,
                LeaseExpiresAt: leaseExpiresAt.StartsWith("0000")
                    ? null
                    : DateTime.ParseExact(leaseExpiresAt, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }
}
