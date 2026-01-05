namespace Conqueror.Iterating.WebSockets;

internal sealed class HttpWebSocketsIteratorJsonSerializer<TIterator, TItem>
    : IHttpWebSocketsIteratorSerializer<TIterator>
    where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
{
    public Task SerializeIterator(
        IServiceProvider serviceProvider,
        TIterator iterator,
        Stream stream,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(stream, iterator, GetIteratorJsonTypeInfo(serviceProvider), cancellationToken);

    public async Task<TIterator> DeserializeIterator(
        IServiceProvider serviceProvider,
        Stream stream,
        CancellationToken cancellationToken
    )
    {
        return await JsonSerializer
                .DeserializeAsync(stream, GetIteratorJsonTypeInfo(serviceProvider), cancellationToken)
                .ConfigureAwait(false)
            ?? throw new InvalidOperationException("failed to deserialize HTTP WebSockets iterator");
    }

    private static JsonTypeInfo<TIterator> GetIteratorJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TIterator>?)
            TIterator.HttpWebSocketsIteratorJsonSerializerContext?.GetTypeInfo(typeof(TIterator));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TIterator>)jsonSerializerSettings.GetTypeInfo(typeof(TIterator));
        }

        return jsonTypeInfo;
    }
}
