namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalPublisher<TSignal>(SignalFileSystemStore fileSystemStore)
    : IFileSystemSignalPublisher<TSignal>
    where TSignal : class, IFileSystemSignal<TSignal>
{
    public string TransportTypeName => TransportName;

    public async Task Publish(
        TSignal signal,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
    {
        try
        {
            Debug.Assert(conquerorContext.SignalId is not null, "signal ID is not null");

            var signalId = new EntryId(conquerorContext.SignalId);
            var signalTag = new Tag(TSignal.Tag);
            var fileExtension = TSignal.FileSystemSignalSerializer.FileExtension;

            await fileSystemStore
                .ContentFiles.WritePayload(
                    signalTag,
                    signalId,
                    fileExtension,
                    static (state, stream, ct) =>
                        TSignal.FileSystemSignalSerializer.SerializeSignal(
                            state.serviceProvider,
                            state.signal,
                            stream,
                            ct
                        ),
                    (serviceProvider, signal),
                    cancellationToken
                )
                .ConfigureAwait(false);

            var encodedContextData = conquerorContext.EncodeDownstreamContextData(
                conquerorContext.TraceId,
                signalId: conquerorContext.SignalId
            );

            fileSystemStore.ContentFiles.WriteMetadata(
                signalTag,
                signalId,
                new(signalId, encodedContextData, TimeProvider.System.GetUtcNow()),
                fileNameSuffix: null,
                SignalMetadataJsonSerializerContext.Default.SignalMetadata,
                cancellationToken
            );

            _ = fileSystemStore.SeqIndexFile.Append(signalId, signalTag, cancellationToken);
        }
        catch (Exception ex) when (ex is not FileSystemSignalFailedOnPublisherException)
        {
            throw new FileSystemSignalFailedOnPublisherException(
                $"file system {nameof(signal)} of type '{typeof(TSignal)}' failed",
                ex
            )
            {
                SignalPayload = signal,
                TransportType = new SignalTransportType(TransportTypeName, SignalTransportRole.Publisher),
            };
        }
    }
}
