using System.Text.Json.Serialization.Metadata;

namespace Conqueror.Transport.FileSystem;

internal sealed class ContentFiles(DirectoryPath baseDirectoryPath)
{
    public async Task WritePayload<TState>(
        Tag tag,
        EntryId entryId,
        string fileExtension,
        Func<TState, Stream, CancellationToken, Task> writeFn,
        TState state,
        CancellationToken cancellationToken)
    {
        var payloadFilePath = GetPayloadFilePath(tag, entryId, fileExtension);

        payloadFilePath.DirectoryPath.EnsureExists();

        var handle = await payloadFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        await writeFn(state, handle.Stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteMetadata<TMetadata>(
        Tag tag,
        EntryId messageId,
        TMetadata metadata,
        string? fileNameSuffix,
        JsonTypeInfo<TMetadata> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var metadataFilePath = GetMetadataFilePath(tag, messageId, fileNameSuffix);

        metadataFilePath.DirectoryPath.AssertExists();

        var handle = await metadataFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        await handle.WriteJson(metadata, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    public async Task<object?> ReadPayload<TState>(
        Tag tag,
        EntryId entryId,
        string fileExtension,
        Func<TState, Stream, CancellationToken, Task<object?>> readFn,
        TState state,
        CancellationToken cancellationToken)
    {
        var payloadFilePath = GetPayloadFilePath(tag, entryId, fileExtension);

        payloadFilePath.DirectoryPath.AssertExists();

        var handle = await payloadFilePath.OpenRead(cancellationToken).ConfigureAwait(false);

        if (handle is null)
        {
            return null;
        }

        await using var handleDisposable = handle.ConfigureAwait(false);

        var result = await readFn(state, handle.Stream, cancellationToken).ConfigureAwait(false);

        return result ?? throw new IOException($"failed to read payload for entry ID '{entryId}' from path '{payloadFilePath}'");
    }

    public bool DoesPayloadExist(Tag tag, EntryId entryId, string fileExtension)
    {
        var payloadFilePath = GetPayloadFilePath(tag, entryId, fileExtension);

        return payloadFilePath.FileExists();
    }

    public async Task<TMetadata> ReadMetadata<TMetadata>(
        Tag tag,
        EntryId messageId,
        string? fileNameSuffix,
        JsonTypeInfo<TMetadata> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var metadataFilePath = GetMetadataFilePath(tag, messageId, fileNameSuffix);

        metadataFilePath.DirectoryPath.AssertExists();

        var handle = await metadataFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        return await handle.ReadJson(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    public async Task<object> WaitForPayload<TState>(
        Tag tag,
        EntryId entryId,
        string fileExtension,
        Func<TState, Stream, CancellationToken, Task<object?>> readFn,
        TState state,
        TimeSpan pollingInterval,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await ReadPayload(
                    tag,
                    entryId,
                    fileExtension,
                    readFn,
                    state,
                    cancellationToken)
                .ConfigureAwait(false);

            if (result is not null)
            {
                return result;
            }

            await Task.Delay(pollingInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new OperationCanceledException();
    }

    private FilePath GetPayloadFilePath(Tag tag, EntryId id, string extension) => baseDirectoryPath.SubDir(tag).File($"{id}{extension}");

    private FilePath GetMetadataFilePath(Tag tag, EntryId id, string? fileNameSuffix)
        => baseDirectoryPath.SubDir(tag).File($"{id}{fileNameSuffix ?? string.Empty}.meta.json");
}
