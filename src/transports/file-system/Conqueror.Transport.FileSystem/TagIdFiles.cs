namespace Conqueror.Transport.FileSystem;

internal sealed class TagIdFiles(DirectoryPath baseDirectoryPath)
{
    private readonly FilePath idsFilePath = baseDirectoryPath.File("tag-ids.json");

    private Dictionary<TagId, Tag> tagCache = [];
    private Dictionary<Tag, TagId> tagIdCache = [];

    public async ValueTask<TagId> GetId(Tag tag, CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        if (tagIdCache.TryGetValue(tag, out var tagId))
        {
            return tagId;
        }

        var handle = await idsFilePath.OpenReadWrite(cancellationToken).ConfigureAwait(false);

        await using var handleDisposable = handle.ConfigureAwait(false);

        if (handle.Stream.Length > 0)
        {
            tagIdCache = await JsonSerializer.DeserializeAsync(
                                                 handle.Stream,
                                                 TagIdsJsonSerializerContext.Default.DictionaryTagTagId,
                                                 cancellationToken)
                                             .ConfigureAwait(false)
                         ?? throw new InvalidOperationException($"failed to deserialize '{tagIdCache}'");

            tagCache = tagIdCache.ToDictionary(pair => pair.Value, pair => pair.Key);

            if (tagIdCache.TryGetValue(tag, out tagId))
            {
                return tagId;
            }
        }

        tagId = new((uint)tagIdCache.Count + 1);

        tagIdCache[tag] = tagId;
        tagCache[tagId] = tag;

        _ = handle.Stream.Seek(0, SeekOrigin.Begin);

        // we do not allow cancellation here to prevent corruption of the file
        await JsonSerializer.SerializeAsync(
                                handle.Stream,
                                tagIdCache,
                                TagIdsJsonSerializerContext.Default.DictionaryTagTagId,
                                CancellationToken.None)
                            .ConfigureAwait(false);

        return tagIdCache[tag];
    }

    public async ValueTask<Tag> GetById(TagId tagId, CancellationToken cancellationToken)
    {
        baseDirectoryPath.AssertExists();

        if (tagCache.TryGetValue(tagId, out var tag))
        {
            return tag;
        }

        var handle = await idsFilePath.OpenRead(cancellationToken).ConfigureAwait(false);

        Debug.Assert(handle is not null, $"handle for file '{idsFilePath}' is null");

        await using var handleDisposable = handle.ConfigureAwait(false);

        tagIdCache = await JsonSerializer.DeserializeAsync(
                                             handle.Stream,
                                             TagIdsJsonSerializerContext.Default.DictionaryTagTagId,
                                             cancellationToken)
                                         .ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"failed to deserialize '{tagIdCache}'");

        tagCache = tagIdCache.ToDictionary(pair => pair.Value, pair => pair.Key);

        if (tagCache.TryGetValue(tagId, out tag))
        {
            return tag;
        }

        throw new InvalidOperationException($"failed to find tag with ID '{tagId}'");
    }
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Dictionary<Tag, TagId>))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(TagId))]
internal sealed partial class TagIdsJsonSerializerContext : JsonSerializerContext;
