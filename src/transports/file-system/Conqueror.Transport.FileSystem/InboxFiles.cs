namespace Conqueror.Transport.FileSystem;

internal sealed class InboxFiles(DirectoryPath baseDirectoryPath, TagIdFiles tagIdFiles)
{
    private const int SeqNrLength = 12; // up to 1 trillion messages
    private const int TagIdLength = 6;
    private const int SeparatorLength = 1;
    private const int StatusLength = 1;
    private const int TimestampLength = 24; // ISO timestamp, e.g. 2025-01-01T12:12:12.123Z

    private const string EmptyTimestamp = "0000-00-00T00:00:00.000Z";

    private const int EntryLength = SeqNrLength
                                    + SeparatorLength
                                    + EntryId.IdLength
                                    + SeparatorLength
                                    + TagIdLength
                                    + SeparatorLength
                                    + StatusLength
                                    + SeparatorLength
                                    + TimestampLength
                                    + 1; // 1 for the newline

    private const char Separator = '|';
    private const char StateMarker = 'M';
    private const char StateAvailable = 'A';
    private const char StateLeased = 'L';

    private event OnAppendHandler? OnAppend;

    public async Task Append(
        InboxName inboxName,
        SeqNr seqNr,
        EntryId id,
        Tag tag,
        CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        var tagId = await tagIdFiles.GetId(tag, cancellationToken).ConfigureAwait(false);

        var inboxFilePath = GetInboxFilePath(inboxName);

        inboxFilePath.DirectoryPath.EnsureExists();

        var handle = await inboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        // if we just created the file, we need to create the initial marker entry
        if (handle.Stream.Length == 0)
        {
            var emptyId = new string('_', EntryId.IdLength);
            var emptyTagId = new string('_', TagIdLength);
            var markerEntry =
                $"{seqNr.ToPaddedString(SeqNrLength)}{Separator}{emptyId}{Separator}{emptyTagId}{Separator}{StateMarker}{Separator}{EmptyTimestamp}\n";

            Debug.Assert(markerEntry.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {markerEntry.Length}");

            // we do not allow cancellation here to prevent corruption of the file
            await handle.Writer.WriteAsync(markerEntry.AsMemory(), CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            // otherwise we override the marker with the new seq nr (if it is not a duplicate)
            var buffer = new char[SeqNrLength];
            var readChars = await handle.Reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            Debug.Assert(readChars == SeqNrLength, $"expected to read {SeqNrLength} chars, but got {readChars}");

            var prevSeqNr = ulong.Parse(buffer);

            Debug.Assert(seqNr > prevSeqNr, $"expected seq nr to be greater than {prevSeqNr}, but it was {seqNr}");

            var seekResult = handle.Stream.Seek(0, SeekOrigin.Begin);

            Debug.Assert(seekResult == 0, $"expected to seek to start of file, but got {seekResult}");

            // we do not allow cancellation here to prevent corruption of the file
            await handle.Writer.WriteAsync(seqNr.ToPaddedString(SeqNrLength).AsMemory(), CancellationToken.None).ConfigureAwait(false);
        }

        await handle.Writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);

        _ = handle.Stream.Seek(0, SeekOrigin.End);

        var entry =
            $"{seqNr.ToPaddedString(SeqNrLength)}{Separator}{id}{Separator}{tagId.ToPaddedString(TagIdLength)}{Separator}{StateAvailable}{Separator}{EmptyTimestamp}\n";

        Debug.Assert(entry.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {entry.Length}");

        // we do not allow cancellation here to prevent corruption of the file
        await handle.Writer.WriteAsync(entry.AsMemory(), CancellationToken.None).ConfigureAwait(false);

        OnAppend?.Invoke(inboxName);
    }

    [SuppressMessage(
        "ReSharper",
        "PossiblyMistakenUseOfCancellationToken",
        Justification = "we are using different tokens for different purposes")]
    public async IAsyncEnumerable<(Tag Tag, SeqNr SeqNr, EntryId Id)> LeaseNextMessage(
        InboxName inboxName,
        TimeSpan pollingInterval,
        TimeSpan? leaseDuration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var inboxFilePath = GetInboxFilePath(inboxName);

        using var appendNotificationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        [SuppressMessage(
            "ReSharper",
            "AccessToDisposedClosure",
            Justification = "false positive, the event handler is removed before the cts is disposed")]
        void NotifyOnAppend(InboxName inboxNameFromEvent)
        {
            if (inboxNameFromEvent == inboxName)
            {
                appendNotificationCts.Cancel();
            }
        }

        OnAppend += NotifyOnAppend;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var leaseExpiresAt = now + leaseDuration;

                (Tag Tag, SeqNr SeqNr, EntryId Id)? nextEntryToYield = null;

                // we only acquire the file lock when the file exists and has at least one entry (ignoring the marker entry)
                if (inboxFilePath.GetFileInfo() is not { Exists: true, Length: > EntryLength })
                {
                    await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);

                    continue;
                }

                var handle = await inboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

                await using (handle.ConfigureAwait(false))
                {
                    var nextAvailableEntry = await FindNextAvailableEntry(
                            handle,
                            now,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (nextAvailableEntry is { Entry: var entry, EntryLineNrInFile: var entryLineNrInFile })
                    {
#if DEBUG
                        Console.WriteLine($"{nextAvailableEntry}");
#endif

                        Debug.Assert(handle is not null, $"expected the inbox file handle for file '{inboxFilePath}' to be non-null");

                        await UpdateInboxEntry(
                                handle,
                                entryLineNrInFile,
                                entry with { State = StateLeased, LeaseExpiresAt = leaseExpiresAt })
                            .ConfigureAwait(false);

                        var tag = await tagIdFiles.GetById(entry.TagId, cancellationToken).ConfigureAwait(false);

                        nextEntryToYield = (tag, entry.SeqNr, entry.Id);
                    }
                }

                if (nextEntryToYield is not null)
                {
                    yield return nextEntryToYield.Value;

                    continue; // try eagerly leasing the next message instead of waiting
                }

                _ = appendNotificationCts.TryReset();

                try
                {
                    await Task.Delay(pollingInterval, appendNotificationCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // if the poll got canceled, but the cancellation token is not canceled, then
                    // this was due to an append-notification ad we simply continue to the next loop
                }
            }

            static async Task<(Entry Entry, int EntryLineNrInFile)?> FindNextAvailableEntry(
                ReadWriteFileHandle handle,
                DateTime now,
                CancellationToken cancellationToken)
            {
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
                    var entryIsAvailable = entry.State == StateAvailable || now >= entry.LeaseExpiresAt;

                    if (entryIsAvailable)
                    {
                        return (entry, entryLineNrInFile);
                    }

                    entryLineNrInFile += 1;
                }

                return null;
            }
        }
        finally
        {
            OnAppend -= NotifyOnAppend;
        }
    }

    public async Task<SeqNr> GetCurrentSeqNr(InboxName inboxName, CancellationToken cancellationToken)
    {
        var inboxFilePath = GetInboxFilePath(inboxName);

        var handle = await inboxFilePath.OpenRead(cancellationToken).ConfigureAwait(false);

        if (handle is null)
        {
            return new(0);
        }

        await using var handleDisposable = handle.ConfigureAwait(false);

        using var buffer = new CharBuffer(SeqNrLength);

        var readChars = await handle.Reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);

        Debug.Assert(readChars == SeqNrLength, $"expected to read {EntryLength} chars but got {readChars}");

        return new(ulong.Parse(buffer.Span));
    }

    public async Task GiveUpLease(InboxName inboxName, SeqNr seqNr, CancellationToken cancellationToken)
    {
        var tagInboxFilePath = GetInboxFilePath(inboxName);

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

    public async Task RemoveEntry(InboxName inboxName, SeqNr seqNr, CancellationToken cancellationToken)
    {
        var tagInboxFilePath = GetInboxFilePath(inboxName);

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

    public async Task<IAsyncDisposable> GetWriteLock(
        InboxName inboxName,
        TimeSpan pollingInterval,
        CancellationToken cancellationToken)
    {
        var writeLockFilePath = GetInboxWriteLockFilePath(inboxName);

        ReadOnlyFileHandle? handle = null;

        while (handle == null)
        {
            handle = writeLockFilePath.TryOpenRead();

            if (handle is null)
            {
                await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);
            }
        }

        return handle;
    }

    public async Task<string> GetContent(InboxName inboxName, CancellationToken cancellationToken)
    {
        var tagInboxFilePath = GetInboxFilePath(inboxName);

        var handle = await tagInboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        return await handle.Reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private FilePath GetInboxFilePath(InboxName inboxName) => baseDirectoryPath.File($".{inboxName}.inbox.txt");

    private FilePath GetInboxWriteLockFilePath(InboxName inboxName) => baseDirectoryPath.File($".{inboxName}.write.lock");

    private static async Task UpdateInboxEntry(ReadWriteFileHandle inboxFileHandle, int entryLineNrInFile, Entry entry)
    {
        var seekTo = entryLineNrInFile * EntryLength;
        var res = inboxFileHandle.Stream.Seek(seekTo, SeekOrigin.Begin);

        Debug.Assert(res == seekTo, $"expected to seek to {seekTo}, but seeked to {res}");

        var leaseExpiresAt = entry.LeaseExpiresAt is not null
            ? entry.LeaseExpiresAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
            : EmptyTimestamp;

        var lineContent =
            $"{entry.SeqNr.ToPaddedString(SeqNrLength)}{Separator}{entry.Id}{Separator}{entry.TagId.ToPaddedString(TagIdLength)}{Separator}{entry.State}{Separator}{leaseExpiresAt}\n";

        Debug.Assert(lineContent.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {lineContent.Length}");

        // we do not allow cancellation here to prevent corruption of the file
        await inboxFileHandle.Writer.WriteAsync(lineContent.AsMemory(), CancellationToken.None).ConfigureAwait(false);
        await inboxFileHandle.Writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
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
        TagId TagId,
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

            var tagId = span.Slice(index, TagIdLength);
            index += TagIdLength;

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
                TagId: new(uint.Parse(tagId)),
                State: state,
                LeaseExpiresAt: leaseExpiresAt.StartsWith("0000")
                    ? null
                    : DateTime.ParseExact(leaseExpiresAt, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    private delegate void OnAppendHandler(InboxName inboxName);
}
