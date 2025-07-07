using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageReceiverRunner(
    IConquerorContextAccessor conquerorContextAccessor)
    : IMessageReceiverRunner<FileSystemMessageReceiver>
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller")]
    public ReceiverExecutionHandle RunReceiver(FileSystemMessageReceiver receiver, CancellationToken cancellationToken)
    {
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connectionTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        return new(
            connectionTaskCompletionSource.Task,
            Run(receiver, connectionTaskCompletionSource, linkedSource.Token),
            linkedSource,
            onDispose: null);
    }

    private async Task Run(
        FileSystemMessageReceiver receiver,
        TaskCompletionSource connectionTaskCompletionSource,
        CancellationToken cancellationToken)
    {
        var config = receiver.Configuration ?? throw new InvalidOperationException($"the receiver for handler type '{receiver.HandlerType}' is not enabled");

        var tags = receiver.Tags.Select(t => new Tag(t)).ToArray();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var fileSystemStores = receiver.ServiceProvider.GetRequiredService<FileSystemStores>();
                    var store = fileSystemStores.GetMessageStore(new(config.BaseDirectoryPath));

                    _ = connectionTaskCompletionSource.TrySetResult();

                    cancellationToken.ThrowIfCancellationRequested();

                    await foreach (var (tag, seqNr, id) in store.InboxFiles.LeaseNextMessage(
                                                                    tags,
                                                                    config.PollingInterval,
                                                                    config.LeaseDuration,
                                                                    cancellationToken)
                                                                .ConfigureAwait(false))
                    {
                        var (messageFileExtension, responseFileExtension) = receiver.GetFileExtensions(tag);

                        var message = await store.ContentFiles.ReadPayload(
                                                     tag,
                                                     id,
                                                     messageFileExtension,
                                                     static (s, stream, ct) => s.receiver.ReadMessage(s.tag, stream, ct),
                                                     (tag, receiver),
                                                     cancellationToken)
                                                 .ConfigureAwait(false);

                        var messageMetadata = await store.ContentFiles.ReadMetadata(
                                                             tag,
                                                             id,
                                                             fileNameSuffix: null,
                                                             MessageMetadataJsonSerializerContext.Default.MessageMetadata,
                                                             cancellationToken)
                                                         .ConfigureAwait(false);

                        if (messageMetadata.TimeToLive is { } ttl)
                        {
                            var now = DateTimeOffset.UtcNow;
                            var isExpired = now > messageMetadata.SentAtUtc + ttl;

                            if (isExpired)
                            {
                                // TODO: append to dead letter queue
                                await store.InboxFiles.RemoveEntry(tag, seqNr, cancellationToken).ConfigureAwait(false);

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
                            var updatedMetadata = messageMetadata with { NrOfFailedProcessingAttempts = messageMetadata.NrOfFailedProcessingAttempts + 1 };
                            await store.ContentFiles.WriteMetadata(
                                           tag,
                                           id,
                                           updatedMetadata,
                                           fileNameSuffix: null,
                                           MessageMetadataJsonSerializerContext.Default.MessageMetadata,
                                           CancellationToken.None)
                                       .ConfigureAwait(false);

                            if (updatedMetadata.NrOfFailedProcessingAttempts >= config.LimitNrOfFailedProcessingAttempts)
                            {
                                // TODO: append to dead letter queue
                                await store.InboxFiles.RemoveEntry(tag, seqNr, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await store.InboxFiles.GiveUpLease(tag, seqNr, cancellationToken).ConfigureAwait(false);
                            }

                            throw;
                        }

                        if (response is UnitMessageResponse)
                        {
                            await store.InboxFiles.RemoveEntry(tag, seqNr, cancellationToken).ConfigureAwait(false);

                            continue;
                        }

                        var encodedContextData = conquerorContext.EncodeUpstreamContextData();

                        await store.ContentFiles.WriteMetadata(
                                       tag,
                                       id,
                                       new(id, encodedContextData),
                                       fileNameSuffix: ".response",
                                       MessageMetadataJsonSerializerContext.Default.MessageResponseMetadata,
                                       cancellationToken)
                                   .ConfigureAwait(false);

                        await store.ContentFiles.WritePayload(
                                       tag,
                                       id,
                                       $".response{responseFileExtension}",
                                       static (state, stream, ct) => state.receiver.WriteResponse(
                                           state.tag,
                                           state.response,
                                           stream,
                                           ct),
                                       (tag, receiver, response),
                                       cancellationToken)
                                   .ConfigureAwait(false);

                        await store.InboxFiles.RemoveEntry(tag, seqNr, cancellationToken).ConfigureAwait(false);
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
                catch (MessageReceiverExecutionFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new MessageReceiverExecutionFailedException(
                        $"an exception occured while running receiver for signal handler type '{receiver.HandlerType}'",
                        ex)
                    {
                        HandlerType = receiver.HandlerType,
                        MessageTransportType = new(TransportName, MessageTransportRole.Receiver),
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
