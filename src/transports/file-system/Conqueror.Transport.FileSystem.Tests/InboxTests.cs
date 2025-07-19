using System.Diagnostics;

namespace Conqueror.Transport.FileSystem.Tests;

[TestFixture]
[NonParallelizable]
internal sealed class InboxTests
{
    private readonly DirectoryInfo baseDirectory = FileSystemTestDirectory.Create();

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public async Task GivenInboxFile_WhenWritingToAndReadingFromInboxWithSingleWriterAndManyParallelReaders_ThenAllWritesAndReadsAreSuccessful()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 10));

        var tagIdFiles = new TagIdFiles(new(baseDirectory.FullName));
        var inboxFiles = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

        var inboxName = new InboxName("test");
        var tag = new Tag("test");

        const int nrOfMessages = 1000;
        const int nrOfReaders = 20;

        var entryIds = Enumerable.Range(1, nrOfMessages).Select(_ => new EntryId(ActivitySpanId.CreateRandom().ToHexString())).ToArray();
        var entryIdsToAppend = new ConcurrentQueue<EntryId>(entryIds);
        var receivedEntryIds = new ConcurrentQueue<EntryId>();

        var appendTask = Task.Run(
            async () =>
            {
                var i = 0;
                while (entryIdsToAppend.TryDequeue(out var entryId))
                {
                    await inboxFiles.Append(
                        inboxName,
                        new((ulong)++i),
                        entryId,
                        tag,
                        cts.Token);
                }
            },
            cts.Token);

        var leaseTask = Parallel.ForEachAsync(
            Enumerable.Range(1, nrOfReaders),
            new ParallelOptions { MaxDegreeOfParallelism = nrOfReaders, CancellationToken = cts.Token },
            async (_, ct) =>
            {
                var receiveCount = 0;

                await foreach (var (_, seqNr, id) in inboxFiles.LeaseNextMessage(
                                   inboxName,
                                   pollingInterval: TimeSpan.FromMilliseconds(10),
                                   leaseDuration: TimeSpan.FromSeconds(60),
                                   ct))
                {
                    receivedEntryIds.Enqueue(id);
                    receiveCount += 1;

                    await inboxFiles.RemoveEntry(inboxName, seqNr, ct);

                    if (receiveCount == nrOfMessages / nrOfReaders)
                    {
                        break;
                    }
                }
            });

        await Assert.MultipleAsync(async () =>
        {
            await Assert.ThatAsync(() => Task.WhenAll(appendTask, leaseTask), Throws.Nothing);

            Assert.That(receivedEntryIds, Has.Count.EqualTo(entryIds.Length));
            Assert.That(receivedEntryIds, Is.EquivalentTo(entryIds));

            var inboxContent = await inboxFiles.GetContent(inboxName, CancellationToken.None);
            Assert.That(inboxContent, Is.EqualTo($"{new SeqNr(nrOfMessages).ToPaddedString(12)}|________________|______|M|0000-00-00T00:00:00.000Z\n"));
        });
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public async Task GivenInboxFile_WhenWritingToAndReadingFromInboxWithSingleWriterAndSingleReader_ThenAllWritesAndReadsAreSuccessful()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 10));

        var tagIdFiles = new TagIdFiles(new(baseDirectory.FullName));
        var inboxFiles = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

        var inboxName = new InboxName("test");
        var tag = new Tag("test");

        const int nrOfMessages = 1000;

        var entryIds = Enumerable.Range(1, nrOfMessages).Select(_ => new EntryId(ActivitySpanId.CreateRandom().ToHexString())).ToArray();
        var entryIdsToAppend = new ConcurrentQueue<EntryId>(entryIds);
        var receivedEntryIds = new ConcurrentQueue<EntryId>();

        var appendTask = Task.Run(
            async () =>
            {
                var i = 0;
                while (entryIdsToAppend.TryDequeue(out var entryId))
                {
                    await inboxFiles.Append(
                        inboxName,
                        new((ulong)++i),
                        entryId,
                        tag,
                        cts.Token);
                }
            },
            cts.Token);

        var leaseTask = Task.Run(
            async () =>
            {
                var receiveCount = 0;

                await foreach (var (_, seqNr, id) in inboxFiles.LeaseNextMessage(
                                   inboxName,
                                   pollingInterval: TimeSpan.FromMilliseconds(10),
                                   leaseDuration: null,
                                   cts.Token))
                {
                    receivedEntryIds.Enqueue(id);
                    receiveCount += 1;

                    await inboxFiles.RemoveEntry(inboxName, seqNr, cts.Token);

                    if (receiveCount == nrOfMessages)
                    {
                        break;
                    }
                }
            },
            cts.Token);

        await Assert.MultipleAsync(async () =>
        {
            await Assert.ThatAsync(() => Task.WhenAll(appendTask, leaseTask), Throws.Nothing);

            Assert.That(receivedEntryIds, Has.Count.EqualTo(entryIds.Length));
            Assert.That(receivedEntryIds, Is.EquivalentTo(entryIds));

            var inboxContent = await inboxFiles.GetContent(inboxName, CancellationToken.None);
            Assert.That(inboxContent, Is.EqualTo($"{new SeqNr(nrOfMessages).ToPaddedString(12)}|________________|______|M|0000-00-00T00:00:00.000Z\n"));
        });
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public async Task GivenSeparateInboxFiles_WhenWritingToAndReadingFromInboxWithSingleWriterAndManyParallelReaders_ThenAllWritesAndReadsAreSuccessful()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 10));

        var tagIdFiles = new TagIdFiles(new(baseDirectory.FullName));
        var inboxFilesForAppend = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

        var inboxName = new InboxName("test");
        var tag = new Tag("test");

        const int nrOfMessages = 1000;
        const int nrOfReaders = 20;

        var entryIds = Enumerable.Range(1, nrOfMessages).Select(_ => new EntryId(ActivitySpanId.CreateRandom().ToHexString())).ToArray();
        var entryIdsToAppend = new ConcurrentQueue<EntryId>(entryIds);
        var receivedEntryIds = new ConcurrentQueue<EntryId>();

        var appendTask = Task.Run(
            async () =>
            {
                var i = 0;
                while (entryIdsToAppend.TryDequeue(out var entryId))
                {
                    await inboxFilesForAppend.Append(
                        inboxName,
                        new((ulong)++i),
                        entryId,
                        tag,
                        cts.Token);
                }
            },
            cts.Token);

        var leaseTask = Parallel.ForEachAsync(
            Enumerable.Range(1, nrOfReaders),
            new ParallelOptions { MaxDegreeOfParallelism = nrOfReaders, CancellationToken = cts.Token },
            async (_, ct) =>
            {
                var inboxFilesForLease = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

                var receiveCount = 0;

                await foreach (var (_, seqNr, id) in inboxFilesForLease.LeaseNextMessage(
                                   inboxName,
                                   pollingInterval: TimeSpan.FromMilliseconds(10),
                                   leaseDuration: TimeSpan.FromSeconds(60),
                                   ct))
                {
                    receivedEntryIds.Enqueue(id);
                    receiveCount += 1;

                    await inboxFilesForLease.RemoveEntry(inboxName, seqNr, ct);

                    if (receiveCount == nrOfMessages / nrOfReaders)
                    {
                        break;
                    }
                }
            });

        await Assert.MultipleAsync(async () =>
        {
            await Assert.ThatAsync(() => Task.WhenAll(appendTask, leaseTask), Throws.Nothing);

            Assert.That(receivedEntryIds, Has.Count.EqualTo(entryIds.Length));
            Assert.That(receivedEntryIds, Is.EquivalentTo(entryIds));

            var inboxContent = await inboxFilesForAppend.GetContent(inboxName, CancellationToken.None);
            Assert.That(inboxContent, Is.EqualTo($"{new SeqNr(nrOfMessages).ToPaddedString(12)}|________________|______|M|0000-00-00T00:00:00.000Z\n"));
        });
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public async Task GivenSeparateInboxFiles_WhenWritingToAndReadingFromInboxWithSingleWriterAndSingleReader_ThenAllWritesAndReadsAreSuccessful()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 10));

        var tagIdFiles = new TagIdFiles(new(baseDirectory.FullName));
        var inboxFilesForAppend = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

        var inboxName = new InboxName("test");
        var tag = new Tag("test");

        const int nrOfMessages = 1000;

        var entryIds = Enumerable.Range(1, nrOfMessages).Select(_ => new EntryId(ActivitySpanId.CreateRandom().ToHexString())).ToArray();
        var entryIdsToAppend = new ConcurrentQueue<EntryId>(entryIds);
        var receivedEntryIds = new ConcurrentQueue<EntryId>();

        var appendTask = Task.Run(
            async () =>
            {
                var i = 0;
                while (entryIdsToAppend.TryDequeue(out var entryId))
                {
                    await inboxFilesForAppend.Append(
                        inboxName,
                        new((ulong)++i),
                        entryId,
                        tag,
                        cts.Token);
                }
            },
            cts.Token);

        var leaseTask = Task.Run(
            async () =>
            {
                var inboxFilesForLease = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

                var receiveCount = 0;

                await foreach (var (_, seqNr, id) in inboxFilesForLease.LeaseNextMessage(
                                   inboxName,
                                   pollingInterval: TimeSpan.FromMilliseconds(10),
                                   leaseDuration: TimeSpan.FromSeconds(60),
                                   cts.Token))
                {
                    receivedEntryIds.Enqueue(id);
                    receiveCount += 1;

                    await inboxFilesForLease.RemoveEntry(inboxName, seqNr, cts.Token);

                    if (receiveCount == nrOfMessages)
                    {
                        break;
                    }
                }
            },
            cts.Token);

        await Assert.MultipleAsync(async () =>
        {
            await Assert.ThatAsync(() => Task.WhenAll(appendTask, leaseTask), Throws.Nothing);

            Assert.That(receivedEntryIds, Has.Count.EqualTo(entryIds.Length));
            Assert.That(receivedEntryIds, Is.EquivalentTo(entryIds));

            var inboxContent = await inboxFilesForAppend.GetContent(inboxName, CancellationToken.None);
            Assert.That(inboxContent, Is.EqualTo($"{new SeqNr(nrOfMessages).ToPaddedString(12)}|________________|______|M|0000-00-00T00:00:00.000Z\n"));
        });
    }

    [Test]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public async Task
        GivenSeparateInboxFiles_WhenWritingToAndReadingSequentiallyFromInboxWithSingleWriterAndManyParallelReaders_ThenAllWritesAndReadsAreSuccessful()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 20));

        var tagIdFiles = new TagIdFiles(new(baseDirectory.FullName));
        var inboxFilesForAppend = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

        var inboxName = new InboxName("test");
        var tag = new Tag("test");

        const int nrOfMessages = 1000;
        const int nrOfReaders = 20;

        var entryIds = Enumerable.Range(1, nrOfMessages).Select(_ => new EntryId(ActivitySpanId.CreateRandom().ToHexString())).ToArray();
        var entryIdsToAppend = new ConcurrentQueue<EntryId>(entryIds);
        var receivedEntryIds = new ConcurrentQueue<EntryId>();

        var i = 0;
        while (entryIdsToAppend.TryDequeue(out var entryId))
        {
            await inboxFilesForAppend.Append(
                inboxName,
                new((ulong)++i),
                entryId,
                tag,
                cts.Token);
        }

        await Parallel.ForEachAsync(
            Enumerable.Range(1, nrOfReaders),
            new ParallelOptions { MaxDegreeOfParallelism = nrOfReaders, CancellationToken = cts.Token },
            async (_, ct) =>
            {
                var inboxFilesForLease = new InboxFiles(new(baseDirectory.FullName), tagIdFiles);

                var receiveCount = 0;

                await foreach (var (_, seqNr, id) in inboxFilesForLease.LeaseNextMessage(
                                   inboxName,
                                   pollingInterval: TimeSpan.FromMilliseconds(10),
                                   leaseDuration: TimeSpan.FromSeconds(60),
                                   ct))
                {
                    receivedEntryIds.Enqueue(id);
                    receiveCount += 1;

                    await inboxFilesForLease.RemoveEntry(inboxName, seqNr, ct);

                    if (receiveCount == nrOfMessages / nrOfReaders)
                    {
                        break;
                    }
                }
            });

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(receivedEntryIds, Has.Count.EqualTo(entryIds.Length));
            Assert.That(receivedEntryIds, Is.EquivalentTo(entryIds));

            var inboxContent = await inboxFilesForAppend.GetContent(inboxName, CancellationToken.None);
            Assert.That(inboxContent, Is.EqualTo($"{new SeqNr(nrOfMessages).ToPaddedString(12)}|________________|______|M|0000-00-00T00:00:00.000Z\n"));
        });
    }
}
