namespace Conqueror.Transport.FileSystem;

[SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "for performance")]
[SuppressMessage("ReSharper", "MethodHasAsyncOverloadWithCancellation", Justification = "for performance")]
[SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "for performance")]
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

        using var handle = await inboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        AppendToFile(handle, seqNr, id, tagId);

        OnAppend?.Invoke(inboxName);

        static void AppendToFile(ReadWriteFileHandle handle, SeqNr seqNr, EntryId id, TagId tagId)
        {
            // if we just created the file, we need to create the initial marker entry
            if (handle.Stream.Length == 0)
            {
                var emptyId = new string('_', EntryId.IdLength);
                var emptyTagId = new string('_', TagIdLength);
                var markerEntry =
                    $"{seqNr.ToPaddedString(SeqNrLength)}{Separator}{emptyId}{Separator}{emptyTagId}{Separator}{StateMarker}{Separator}{EmptyTimestamp}\n";

                Debug.Assert(markerEntry.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {markerEntry.Length}");

                // we do not allow cancellation here to prevent corruption of the file
                handle.Writer.Write(markerEntry);
            }
            else
            {
                // otherwise we override the marker with the new seq nr (if it is not a duplicate)
                Span<char> buffer = stackalloc char[SeqNrLength];
                var readChars = handle.Reader.Read(buffer);

                Debug.Assert(readChars == SeqNrLength, $"expected to read {SeqNrLength} chars, but got {readChars}");

                var prevSeqNr = ulong.Parse(buffer);

                Debug.Assert(seqNr > prevSeqNr, $"expected seq nr to be greater than {prevSeqNr}, but it was {seqNr}");

                var seekResult = handle.Stream.Seek(0, SeekOrigin.Begin);

                Debug.Assert(seekResult == 0, $"expected to seek to start of file, but got {seekResult}");

                // we do not allow cancellation here to prevent corruption of the file
                handle.Writer.Write(seqNr.ToPaddedString(SeqNrLength));
            }

            handle.Writer.Flush();

            _ = handle.Stream.Seek(0, SeekOrigin.End);

            var entry =
                $"{seqNr.ToPaddedString(SeqNrLength)}{Separator}{id}{Separator}{tagId.ToPaddedString(TagIdLength)}{Separator}{StateAvailable}{Separator}{EmptyTimestamp}\n";

            Debug.Assert(entry.Length == EntryLength, $"expected entry length to be {EntryLength}, but it was {entry.Length}");

            // we do not allow cancellation here to prevent corruption of the file
            handle.Writer.Write(entry);
        }
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

                using (var handle = await inboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false))
                {
                    var nextAvailableEntry = FindNextAvailableEntry(handle, now);

                    if (nextAvailableEntry is { Entry: var entry, EntryLineNrInFile: var entryLineNrInFile })
                    {
#if DEBUG
                        Console.WriteLine($"{nextAvailableEntry}");
#endif

                        Debug.Assert(handle is not null, $"expected the inbox file handle for file '{inboxFilePath}' to be non-null");

                        UpdateInboxEntry(handle, entryLineNrInFile, entry with { State = StateLeased, LeaseExpiresAt = leaseExpiresAt });

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

            static (Entry Entry, int EntryLineNrInFile)? FindNextAvailableEntry(ReadWriteFileHandle handle, DateTime now)
            {
                ThrowOnInvalidLength(handle);

                // skip marker entry
                _ = handle.Stream.Seek(EntryLength, SeekOrigin.Begin);

                Span<char> buffer = stackalloc char[EntryLength];
                var entryLineNrInFile = 1;

                while (!handle.Reader.EndOfStream)
                {
                    var readBytes = handle.Reader.Read(buffer);

                    Debug.Assert(readBytes == EntryLength, $"expected to read {EntryLength} bytes, but read {readBytes}");

                    var entry = Entry.Parse(buffer);
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

    public async ValueTask<SeqNr> GetCurrentSeqNr(InboxName inboxName, CancellationToken cancellationToken)
    {
        var inboxFilePath = GetInboxFilePath(inboxName);

        using var handle = await inboxFilePath.OpenRead(cancellationToken).ConfigureAwait(false);

        return handle is null ? new(0) : Read(handle);

        static SeqNr Read(ReadOnlyFileHandle handle)
        {
            Span<char> buffer = stackalloc char[SeqNrLength];

            var readChars = handle.Reader.Read(buffer);

            Debug.Assert(readChars == SeqNrLength, $"expected to read {EntryLength} chars but got {readChars}");

            return new(ulong.Parse(buffer));
        }
    }

    public async ValueTask GiveUpLease(InboxName inboxName, SeqNr seqNr, CancellationToken cancellationToken)
    {
        var tagInboxFilePath = GetInboxFilePath(inboxName);

        tagInboxFilePath.DirectoryPath.AssertExists();

        using var handle = await tagInboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        UpdateEntry(handle, seqNr);

        static void UpdateEntry(ReadWriteFileHandle handle, SeqNr seqNr)
        {
            // skip marker entry
            _ = handle.Stream.Seek(EntryLength, SeekOrigin.Begin);

            // TODO: for performance, consider allocating a bigger buffer to read the first few entries at once
            Span<char> buffer = stackalloc char[EntryLength];
            var entryLineNrInFile = 1;

            while (!handle.Reader.EndOfStream)
            {
                var readBytes = handle.Reader.Read(buffer);

                Debug.Assert(readBytes == EntryLength, $"expected to read {EntryLength} bytes, but read {readBytes}");

                var entry = Entry.Parse(buffer);

                if (entry.SeqNr == seqNr)
                {
                    UpdateInboxEntry(handle, entryLineNrInFile, entry with { State = StateAvailable, LeaseExpiresAt = null });

                    return;
                }

                entryLineNrInFile += 1;
            }
        }
    }

    public async ValueTask RemoveEntry(InboxName inboxName, SeqNr seqNr, CancellationToken cancellationToken)
    {
        var tagInboxFilePath = GetInboxFilePath(inboxName);

        tagInboxFilePath.DirectoryPath.AssertExists();

        using var handle = await tagInboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        ThrowOnInvalidLength(handle);

        Remove(handle, seqNr);

        static void Remove(ReadWriteFileHandle handle, SeqNr seqNr)
        {
            // skip marker entry
            _ = handle.Stream.Seek(EntryLength, SeekOrigin.Begin);

            // TODO: for performance, consider allocating a bigger buffer to read the first few entries at once
            Span<char> buffer = stackalloc char[EntryLength];
            var entryLineNrInFile = 1;

            while (!handle.Reader.EndOfStream)
            {
                var readBytes = handle.Reader.Read(buffer);

                Debug.Assert(readBytes == EntryLength, $"expected to read {EntryLength} bytes, but read {readBytes}");

                var entry = Entry.Parse(buffer);

                if (entry.SeqNr == seqNr)
                {
                    FileOperations.DeleteLineFromFile(handle, entryLineNrInFile, EntryLength);

                    return;
                }

                entryLineNrInFile += 1;
            }
        }
    }

    public async ValueTask<IDisposable> GetWriteLock(
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

    public async ValueTask<string> GetContent(InboxName inboxName, CancellationToken cancellationToken)
    {
        var tagInboxFilePath = GetInboxFilePath(inboxName);

        using var handle = await tagInboxFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        return handle.Reader.ReadToEnd();
    }

    private FilePath GetInboxFilePath(InboxName inboxName) => baseDirectoryPath.File($".{inboxName}.inbox.txt");

    private FilePath GetInboxWriteLockFilePath(InboxName inboxName) => baseDirectoryPath.File($".{inboxName}.write.lock");

    private static void UpdateInboxEntry(ReadWriteFileHandle inboxFileHandle, int entryLineNrInFile, Entry entry)
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
        inboxFileHandle.Writer.Write(lineContent);
        inboxFileHandle.Writer.Flush();
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
