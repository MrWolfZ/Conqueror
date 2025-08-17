namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalReceiverRunner(IConquerorContextAccessor conquerorContextAccessor)
    : ISignalReceiverRunner<FileSystemSignalReceiver>
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller"
    )]
    public ReceiverExecutionHandle RunReceiver(FileSystemSignalReceiver receiver, CancellationToken cancellationToken)
    {
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connectionTaskCompletionSource = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        return new ReceiverExecutionHandle(
            connectionTaskCompletionSource.Task,
            Run(receiver, connectionTaskCompletionSource, linkedSource.Token),
            linkedSource,
            onDispose: null
        );
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    private async Task Run(
        FileSystemSignalReceiver receiver,
        TaskCompletionSource connectionTaskCompletionSource,
        CancellationToken cancellationToken
    )
    {
        var config =
            receiver.Configuration
            ?? throw new InvalidOperationException(
                $"the receiver for handler type '{receiver.HandlerType}' is not enabled"
            );

        var tags = receiver.Tags.Select(t => new Tag(t)).ToArray();

        var inboxName = new InboxName(config.Name);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Task? seqFileProcessingTask = null;
                Task? inboxFileProcessingTask = null;

                try
                {
                    var fileSystemStores = receiver.ServiceProvider.GetRequiredService<FileSystemStores>();
                    var store = fileSystemStores.GetSignalStore(new(config.BaseDirectoryPath));
                    var inboxFiles = store.GetInboxFiles();

                    cancellationToken.ThrowIfCancellationRequested();

                    seqFileProcessingTask = ProcessSeqFileChanges(
                        store.SeqIndexFile,
                        inboxFiles,
                        inboxName,
                        tags,
                        config.PollingInterval,
                        cts.Token
                    );

                    inboxFileProcessingTask = ProcessInbox(
                        receiver,
                        inboxFiles,
                        store.ContentFiles,
                        inboxName,
                        config.PollingInterval,
                        config.LeaseDuration,
                        config.SignalCallback,
                        cts.Token
                    );

                    _ = connectionTaskCompletionSource.TrySetResult();

                    // if any of the two processing tasks completes, we need to stop
                    var completedTask = await Task.WhenAny(seqFileProcessingTask, inboxFileProcessingTask)
                        .ConfigureAwait(false);

                    // await the completed task to propagate any exception
                    await completedTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // we return gracefully on cancellation
                }
                catch (IOException) when (cancellationToken.IsCancellationRequested)
                {
                    // the file system access might throw an IOException instead of an OperationCanceledException when
                    // the token is canceled, so we catch it here and return gracefully
                }
                catch (SignalReceiverExecutionFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SignalReceiverExecutionFailedException(
                        $"an exception occured while running {nameof(receiver)} for signal handler type '{receiver.HandlerType}'",
                        ex
                    )
                    {
                        HandlerType = receiver.HandlerType,
                        SignalTransportType = new SignalTransportType(TransportName, SignalTransportRole.Receiver),
                    };
                }
                finally
                {
                    await cts.CancelAsync().ConfigureAwait(false);

                    if (seqFileProcessingTask is { IsCompleted: false })
                    {
                        await seqFileProcessingTask.ConfigureAwait(false);
                    }

                    if (inboxFileProcessingTask is { IsCompleted: false })
                    {
                        await inboxFileProcessingTask.ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            config.ExceptionCallback?.Invoke(ex);

            _ = connectionTaskCompletionSource.TrySetException(ex);

            throw;
        }
        finally
        {
            _ = connectionTaskCompletionSource.TrySetResult();
        }
    }

    private static async Task ProcessSeqFileChanges(
        SeqIndexFile seqIndexFile,
        InboxFiles inboxFiles,
        InboxName inboxName,
        Tag[] tags,
        TimeSpan pollingInterval,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var disposable = await inboxFiles
                .GetWriteLock(inboxName, pollingInterval, cancellationToken)
                .ConfigureAwait(false);

            var startAtSeqNr = inboxFiles.GetCurrentSeqNr(inboxName, cancellationToken);

            var latestSeqNr = new SeqNr(0);

            await foreach (
                var entry in seqIndexFile
                    .ReadChanges(startAtSeqNr, pollingInterval, cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                foreach (var (newEntryId, signalTag, seqNr) in entry)
                {
                    Debug.Assert(
                        seqNr > latestSeqNr,
                        $"expected the next seq nr {seqNr} to be greater than latest seq nr {latestSeqNr}"
                    );

                    latestSeqNr = seqNr;

                    var isRelevantEntry = tags.Contains(signalTag);

                    if (isRelevantEntry)
                    {
                        inboxFiles.Append(inboxName, seqNr, newEntryId, signalTag, cancellationToken);
                    }
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // we gracefully exit
        }
    }

    private async Task ProcessInbox(
        FileSystemSignalReceiver receiver,
        InboxFiles inboxFiles,
        ContentFiles contentFiles,
        InboxName inboxName,
        TimeSpan pollingInterval,
        TimeSpan? leaseDuration,
        Action<object>? signalCallback,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (
                var (tag, seqNr, entryId) in inboxFiles
                    .LeaseNextMessage(inboxName, pollingInterval, leaseDuration, cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                try
                {
                    var fileExtension = receiver.GetFileExtension(tag);

                    var signal = await contentFiles
                        .ReadPayload(
                            tag,
                            entryId,
                            fileExtension,
                            static (s, stream, ct) => s.receiver.ReadSignal(s.tag, stream, ct),
                            (tag, receiver),
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    Debug.Assert(
                        signal is not null,
                        $"the signal payload file for tag '{tag}' and ID '{entryId}' should exist"
                    );

                    var metadata = contentFiles.ReadMetadata(
                        tag,
                        entryId,
                        fileNameSuffix: null,
                        SignalMetadataJsonSerializerContext.Default.SignalMetadata,
                        cancellationToken
                    );

                    using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

                    if (metadata.EncodedContextData is not null)
                    {
                        conquerorContext.DecodeContextData(metadata.EncodedContextData);
                    }

                    signalCallback?.Invoke(signal);

                    await receiver.InvokeHandler(signal, cancellationToken).ConfigureAwait(false);

                    inboxFiles.RemoveEntry(inboxName, seqNr, cancellationToken);
                }
                catch
                {
                    if (leaseDuration is not null)
                    {
                        inboxFiles.GiveUpLease(inboxName, seqNr, cancellationToken);
                    }

                    throw;
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // we gracefully exit
        }
    }
}
