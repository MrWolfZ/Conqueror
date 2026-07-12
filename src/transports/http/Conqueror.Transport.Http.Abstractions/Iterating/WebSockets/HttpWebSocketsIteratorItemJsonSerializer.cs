namespace Conqueror.Iterating.WebSockets;

internal sealed class HttpWebSocketsIteratorItemJsonSerializer<TIterator, TItem> : IHttpWebSocketsItemSerializer<TItem>
    where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
{
    public Task SerializeItem(
        IServiceProvider serviceProvider,
        TItem item,
        Stream stream,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(stream, item, GetItemJsonTypeInfo(serviceProvider), cancellationToken);

    public async Task<TItem> DeserializeItem(
        IServiceProvider serviceProvider,
        Stream stream,
        CancellationToken cancellationToken
    )
    {
        return await JsonSerializer
                   .DeserializeAsync(stream, GetItemJsonTypeInfo(serviceProvider), cancellationToken)
                   .ConfigureAwait(false)
               ?? throw new InvalidOperationException("failed to deserialize HTTP WebSockets iterator item");
    }

    private static JsonTypeInfo<TItem> GetItemJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TItem>?)
            TIterator.HttpWebSocketsItemJsonSerializerContext?.GetTypeInfo(typeof(TItem));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TItem>)jsonSerializerSettings.GetTypeInfo(typeof(TItem));
        }

        return jsonTypeInfo;
    }
}
