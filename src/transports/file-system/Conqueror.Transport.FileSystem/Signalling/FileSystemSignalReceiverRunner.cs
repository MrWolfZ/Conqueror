using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalReceiverRunner(
    IConquerorContextAccessor conquerorContextAccessor)
    : ISignalReceiverRunner<FileSystemSignalReceiver>
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller")]
    public ReceiverExecutionHandle RunReceiver(FileSystemSignalReceiver receiver, CancellationToken cancellationToken)
    {
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connectionTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        return new(
            connectionTaskCompletionSource.Task,
            Run(receiver, connectionTaskCompletionSource, linkedSource.Token),
            linkedSource,
            onDispose: null);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    private async Task Run(
        FileSystemSignalReceiver receiver,
        TaskCompletionSource connectionTaskCompletionSource,
        CancellationToken cancellationToken)
    {
        var config = receiver.Configuration ?? throw new InvalidOperationException($"the receiver for handler type '{receiver.HandlerType}' is not enabled");

        var tags = receiver.Tags.Select(t => new Tag(t)).ToArray();

        // we are abusing the tag mechanism a bit by creating a tag for the receiver to be able to create
        // an inbox for the receiver based on this tag; this is in contrast to messages where there is one
        // inbox per message tag
        var receiverTag = new Tag(config.Name);

        var latestSeqNr = new SeqNr(0);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var fileSystemStores = receiver.ServiceProvider.GetRequiredService<FileSystemStores>();
                    var store = fileSystemStores.GetSignalStore(new(config.BaseDirectoryPath));
                    using var inboxFiles = store.GetInboxFiles();

                    _ = connectionTaskCompletionSource.TrySetResult();

                    cancellationToken.ThrowIfCancellationRequested();

                    var startAtSeqNr = await inboxFiles.GetCurrentSeqNr(receiverTag, cancellationToken).ConfigureAwait(false);

                    var seqFileEnumerator = store.SeqIndexFile.ReadChanges(
                                                     startAtSeqNr,
                                                     config.PollingInterval,
                                                     cancellationToken)
                                                 .GetAsyncEnumerator(cancellationToken);

                    var inboxEnumerator = inboxFiles.LeaseNextMessage(
                                                   [receiverTag],
                                                   config.PollingInterval,
                                                   config.LeaseDuration,
                                                   cancellationToken)
                                               .GetAsyncEnumerator(cancellationToken);

                    var seqFileTask = seqFileEnumerator.MoveNextAsync().AsTask();
                    var inboxFileTask = inboxEnumerator.MoveNextAsync().AsTask();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var completedTask = await Task.WhenAny(seqFileTask, inboxFileTask).WaitAsync(cancellationToken).ConfigureAwait(false);

                        var hasNext = await completedTask.ConfigureAwait(false);

                        // the only case in which one of the enumerators will return `false` is when the enumeration was
                        // cancelled, in which case we can simply stop here
                        if (!hasNext)
                        {
                            Debug.Assert(cancellationToken.IsCancellationRequested, "expected cancellation, but was not cancelled");

                            return;
                        }

                        if (completedTask == seqFileTask)
                        {
                            var (newEntryId, seqNr) = seqFileEnumerator.Current;

                            Debug.Assert(seqNr > latestSeqNr, $"expected the next seq nr {seqNr} to be greater than latest seq nr {latestSeqNr}");

                            latestSeqNr = seqNr;

                            var isRelevantEntry = tags.Any(t => store.ContentFiles.DoesPayloadExist(t, newEntryId, receiver.GetFileExtension(t)));

                            if (isRelevantEntry)
                            {
                                await inboxFiles.Append(
                                                    receiverTag,
                                                    seqNr,
                                                    newEntryId,
                                                    cancellationToken)
                                                .ConfigureAwait(false);
                            }

                            seqFileTask = seqFileEnumerator.MoveNextAsync().AsTask();
                        }
                        else
                        {
                            var (_, seqNr, entryId) = inboxEnumerator.Current;

                            try
                            {
                                var tag = tags.First(t => store.ContentFiles.DoesPayloadExist(t, entryId, receiver.GetFileExtension(t)));

                                var fileExtension = receiver.GetFileExtension(tag);

                                var signal = await store.ContentFiles.ReadPayload(
                                                            tag,
                                                            entryId,
                                                            fileExtension,
                                                            static (s, stream, ct) => s.receiver.ReadSignal(s.tag, stream, ct),
                                                            (tag, receiver),
                                                            cancellationToken)
                                                        .ConfigureAwait(false);

                                Debug.Assert(signal is not null, $"the signal payload file for tag '{tag}' and ID '{entryId}' should exist");

                                var metadata = await store.ContentFiles.ReadMetadata(
                                                              tag,
                                                              entryId,
                                                              fileNameSuffix: null,
                                                              SignalMetadataJsonSerializerContext.Default.SignalMetadata,
                                                              cancellationToken)
                                                          .ConfigureAwait(false);

                                using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

                                if (metadata.EncodedContextData is not null)
                                {
                                    conquerorContext.DecodeContextData(metadata.EncodedContextData);
                                }

                                config.SignalCallback?.Invoke(signal);

                                await receiver.InvokeHandler(signal, cancellationToken).ConfigureAwait(false);

                                await inboxFiles.RemoveEntry(receiverTag, seqNr, cancellationToken).ConfigureAwait(false);
                            }
                            catch
                            {
                                await inboxFiles.GiveUpLease(receiverTag, seqNr, cancellationToken).ConfigureAwait(false);

                                throw;
                            }

                            inboxFileTask = inboxEnumerator.MoveNextAsync().AsTask();
                        }
                    }
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
                        $"an exception occured while running receiver for signal handler type '{receiver.HandlerType}'",
                        ex)
                    {
                        HandlerType = receiver.HandlerType,
                        SignalTransportType = new(TransportName, SignalTransportRole.Receiver),
                    };
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
}
