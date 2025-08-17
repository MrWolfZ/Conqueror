namespace Conqueror.Transport.FileSystem;

internal sealed class ContentFiles(DirectoryPath baseDirectoryPath)
{
    public async ValueTask WritePayload<TState>(
        Tag tag,
        EntryId entryId,
        string fileExtension,
        Func<TState, Stream, CancellationToken, Task> writeFn,
        TState state,
        CancellationToken cancellationToken
    )
    {
        var payloadFilePath = GetPayloadFilePath(tag, entryId, fileExtension);

        payloadFilePath.DirectoryPath.EnsureExists();

        using var handle = payloadFilePath.OpenReadWrite(cancellationToken);

        await writeFn(state, handle.Stream, cancellationToken).ConfigureAwait(false);
    }

    public void WriteMetadata<TMetadata>(
        Tag tag,
        EntryId messageId,
        TMetadata metadata,
        string? fileNameSuffix,
        JsonTypeInfo<TMetadata> jsonTypeInfo,
        CancellationToken cancellationToken
    )
    {
        var metadataFilePath = GetMetadataFilePath(tag, messageId, fileNameSuffix);

        metadataFilePath.DirectoryPath.AssertExists();

        using var handle = metadataFilePath.OpenReadWrite(cancellationToken);

        handle.WriteJson(metadata, jsonTypeInfo);
    }

    public async ValueTask<object?> ReadPayload<TState>(
        Tag tag,
        EntryId entryId,
        string fileExtension,
        Func<TState, Stream, CancellationToken, Task<object?>> readFn,
        TState state,
        CancellationToken cancellationToken
    )
    {
        var payloadFilePath = GetPayloadFilePath(tag, entryId, fileExtension);

        payloadFilePath.DirectoryPath.AssertExists();

        using var handle = payloadFilePath.OpenRead(cancellationToken);

        if (handle is null)
        {
            return null;
        }

        var result = await readFn(state, handle.Stream, cancellationToken).ConfigureAwait(false);

        return result
            ?? throw new IOException($"failed to read payload for entry ID '{entryId}' from path '{payloadFilePath}'");
    }

    public TMetadata ReadMetadata<TMetadata>(
        Tag tag,
        EntryId messageId,
        string? fileNameSuffix,
        JsonTypeInfo<TMetadata> jsonTypeInfo,
        CancellationToken cancellationToken
    )
    {
        var metadataFilePath = GetMetadataFilePath(tag, messageId, fileNameSuffix);

        metadataFilePath.DirectoryPath.AssertExists();

        using var handle = metadataFilePath.OpenReadWrite(cancellationToken);

        return handle.ReadJson(jsonTypeInfo);
    }

    public async ValueTask<object> WaitForPayload<TState>(
        Tag tag,
        EntryId entryId,
        string fileExtension,
        Func<TState, Stream, CancellationToken, Task<object?>> readFn,
        TState state,
        TimeSpan pollingInterval,
        CancellationToken cancellationToken
    )
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await ReadPayload(tag, entryId, fileExtension, readFn, state, cancellationToken)
                .ConfigureAwait(false);

            if (result is not null)
            {
                return result;
            }

            await Task.Delay(pollingInterval, TimeProvider.System, cancellationToken).ConfigureAwait(false);
        }

        throw new OperationCanceledException();
    }

    private FilePath GetPayloadFilePath(Tag tag, EntryId id, string extension) =>
        baseDirectoryPath.SubDir(tag).File($"{id}{extension}");

    private FilePath GetMetadataFilePath(Tag tag, EntryId id, string? fileNameSuffix) =>
        baseDirectoryPath.SubDir(tag).File($"{id}{fileNameSuffix ?? ""}.meta.json");
}
