namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageSender<TMessage, TResponse>(
    MessageFileSystemStore fileSystemStore,
    TimeSpan pollingInterval)
    : IFileSystemMessageSender<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    private TimeSpan? configuredTimeToLive;

    public string TransportTypeName => TransportName;

    public async Task<TResponse> Send(
        TMessage message,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        try
        {
            Debug.Assert(conquerorContext.MessageId is not null, "message ID is not null");

            var messageId = new EntryId(conquerorContext.MessageId);
            var messageTag = new Tag(TMessage.Tag);
            var fileExtension = TMessage.FileSystemMessageSerializer.FileExtension;
            var responseFileExtension = $".response{TMessage.FileSystemMessageResponseSerializer.FileExtension}";

            await fileSystemStore.ContentFiles.WritePayload(
                                     messageTag,
                                     messageId,
                                     fileExtension,
                                     static (state, stream, ct) => TMessage.FileSystemMessageSerializer.SerializeMessage(
                                         state.serviceProvider,
                                         state.message,
                                         stream,
                                         ct),
                                     (serviceProvider, message),
                                     cancellationToken)
                                 .ConfigureAwait(false);

            var encodedContextData = conquerorContext.EncodeDownstreamContextData(
                traceId: conquerorContext.TraceId,
                messageId: conquerorContext.MessageId);

            await fileSystemStore.ContentFiles.WriteMetadata(
                                     messageTag,
                                     messageId,
                                     new(
                                         messageId,
                                         encodedContextData,
                                         SentAtUtc: DateTimeOffset.UtcNow,
                                         configuredTimeToLive,
                                         NrOfFailedProcessingAttempts: 0),
                                     fileNameSuffix: null,
                                     MessageMetadataJsonSerializerContext.Default.MessageMetadata,
                                     cancellationToken)
                                 .ConfigureAwait(false);

            var seqNr = await fileSystemStore.SeqIndexFile.Append(messageId, cancellationToken).ConfigureAwait(false);

            await fileSystemStore.InboxFiles.Append(
                                     messageTag,
                                     seqNr,
                                     messageId,
                                     cancellationToken)
                                 .ConfigureAwait(false);

            if (typeof(TResponse) == typeof(UnitMessageResponse))
            {
                return (TResponse)(object)UnitMessageResponse.Instance;
            }

            var response = await fileSystemStore.ContentFiles.WaitForPayload(
                                                    messageTag,
                                                    messageId,
                                                    responseFileExtension,
                                                    static async (p, stream, ct) => await TMessage.FileSystemMessageResponseSerializer.DeserializeResponse(
                                                        p,
                                                        stream,
                                                        ct).ConfigureAwait(false),
                                                    serviceProvider,
                                                    pollingInterval,
                                                    cancellationToken)
                                                .ConfigureAwait(false);

            var responseMetadata = await fileSystemStore.ContentFiles.ReadMetadata(
                                                            messageTag,
                                                            messageId,
                                                            fileNameSuffix: ".response",
                                                            MessageMetadataJsonSerializerContext.Default.MessageResponseMetadata,
                                                            cancellationToken)
                                                        .ConfigureAwait(false);

            if (responseMetadata.EncodedContextData is not null)
            {
                conquerorContext.DecodeContextData(responseMetadata.EncodedContextData);
            }

            return (TResponse)response;
        }
        catch (Exception ex) when (ex is not FileSystemMessageFailedOnSenderException)
        {
            throw new FileSystemMessageFailedOnSenderException($"file system message of type '{typeof(TMessage)}' failed", ex)
            {
                MessagePayload = message,
                TransportType = new(TransportTypeName, MessageTransportRole.Sender),
            };
        }
    }

    public IFileSystemMessageSender<TMessage, TResponse> WithTimeToLive(TimeSpan timeToLive)
    {
        configuredTimeToLive = timeToLive;

        return this;
    }
}
