namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageReceiverRunner(IConquerorContextAccessor conquerorContextAccessor)
    : IMessageReceiverRunner<FileSystemMessageReceiver>
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller"
    )]
    public ReceiverExecutionHandle RunReceiver(FileSystemMessageReceiver receiver, CancellationToken cancellationToken)
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
        FileSystemMessageReceiver receiver,
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
                    var store = fileSystemStores.GetMessageStore(new(config.BaseDirectoryPath));
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
                        config,
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
                catch (MessageReceiverExecutionFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new MessageReceiverExecutionFailedException(
                        $"an exception occured while running {nameof(receiver)} for message handler type '{receiver.HandlerType}'",
                        ex
                    )
                    {
                        HandlerType = receiver.HandlerType,
                        MessageTransportType = new MessageTransportType(TransportName, MessageTransportRole.Receiver),
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
                foreach (var (newEntryId, messageTag, seqNr) in entry)
                {
                    Debug.Assert(
                        seqNr > latestSeqNr,
                        $"expected the next seq nr {seqNr} to be greater than latest seq nr {latestSeqNr}"
                    );

                    latestSeqNr = seqNr;

                    var isRelevantEntry = tags.Contains(messageTag);

                    if (isRelevantEntry)
                    {
                        inboxFiles.Append(inboxName, seqNr, newEntryId, messageTag, cancellationToken);
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
        FileSystemMessageReceiver receiver,
        InboxFiles inboxFiles,
        ContentFiles contentFiles,
        InboxName inboxName,
        FileSystemMessageReceiverConfiguration config,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await foreach (
                var (tag, seqNr, entryId) in inboxFiles
                    .LeaseNextMessage(inboxName, config.PollingInterval, config.LeaseDuration, cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                try
                {
                    var (messageFileExtension, responseFileExtension) = receiver.GetFileExtensions(tag);

                    var message = await contentFiles
                        .ReadPayload(
                            tag,
                            entryId,
                            messageFileExtension,
                            static (s, stream, ct) => s.receiver.ReadMessage(s.tag, stream, ct),
                            (tag, receiver),
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    Debug.Assert(
                        message is not null,
                        $"the message payload file for tag '{tag}' and ID '{entryId}' should exist"
                    );

                    var messageMetadata = contentFiles.ReadMetadata(
                        tag,
                        entryId,
                        fileNameSuffix: null,
                        MessageMetadataJsonSerializerContext.Default.MessageMetadata,
                        cancellationToken
                    );

                    if (messageMetadata.TimeToLive is { } ttl)
                    {
                        var now = TimeProvider.System.GetUtcNow();
                        var isExpired = now > messageMetadata.SentAtUtc + ttl;

                        if (isExpired)
                        {
                            // TODO: append to dead letter queue
                            inboxFiles.RemoveEntry(inboxName, seqNr, cancellationToken);

                            continue;
                        }
                    }

                    using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

                    if (messageMetadata.EncodedContextData is not null)
                    {
                        conquerorContext.DecodeContextData(messageMetadata.EncodedContextData);
                    }

                    object response;

                    try
                    {
                        config.MessageCallback?.Invoke(message);

                        response = await receiver.InvokeHandler(tag, message, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        var updatedMetadata = messageMetadata with
                        {
                            NrOfFailedProcessingAttempts = messageMetadata.NrOfFailedProcessingAttempts + 1,
                        };
                        contentFiles.WriteMetadata(
                            tag,
                            entryId,
                            updatedMetadata,
                            fileNameSuffix: null,
                            MessageMetadataJsonSerializerContext.Default.MessageMetadata,
                            CancellationToken.None
                        );

                        if (updatedMetadata.NrOfFailedProcessingAttempts >= config.LimitNrOfFailedProcessingAttempts)
                        {
                            // TODO: append to dead letter queue
                            inboxFiles.RemoveEntry(inboxName, seqNr, cancellationToken);
                        }
                        else if (config.LeaseDuration is not null)
                        {
                            inboxFiles.GiveUpLease(inboxName, seqNr, cancellationToken);
                        }
                        else
                        {
                            // if the lease duration is null, it means we are the single reader, so the next loop
                            // iteration will simply process the message again
                        }

                        throw;
                    }

                    if (response is UnitMessageResponse)
                    {
                        inboxFiles.RemoveEntry(inboxName, seqNr, cancellationToken);

                        continue;
                    }

                    var encodedContextData = conquerorContext.EncodeUpstreamContextData();

                    contentFiles.WriteMetadata(
                        tag,
                        entryId,
                        new(entryId, encodedContextData),
                        ".response",
                        MessageMetadataJsonSerializerContext.Default.MessageResponseMetadata,
                        cancellationToken
                    );

                    await contentFiles
                        .WritePayload(
                            tag,
                            entryId,
                            $".response{responseFileExtension}",
                            static (state, stream, ct) =>
                                state.receiver.WriteResponse(state.tag, state.response, stream, ct),
                            (tag, receiver, response),
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    inboxFiles.RemoveEntry(inboxName, seqNr, cancellationToken);
                }
                catch
                {
                    if (config.LeaseDuration is not null)
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
