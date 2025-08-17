namespace Conqueror.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalJsonSerializer<TSignal> : IHttpWebSocketsSignalSerializer<TSignal>
    where TSignal : class, IHttpWebSocketsSignal<TSignal>
{
    public Task SerializeSignal(
        IServiceProvider serviceProvider,
        TSignal signal,
        Stream stream,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(stream, signal, GetJsonTypeInfo(serviceProvider), cancellationToken);

    public async Task<TSignal> DeserializeSignal(
        IServiceProvider serviceProvider,
        Stream stream,
        CancellationToken cancellationToken
    )
    {
        return await JsonSerializer
                .DeserializeAsync(stream, GetJsonTypeInfo(serviceProvider), cancellationToken)
                .ConfigureAwait(false)
            ?? throw new InvalidOperationException("failed to deserialize HTTP WebSockets signal");
    }

    private static JsonTypeInfo<TSignal> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TSignal>?)
            TSignal.HttpWebSocketsJsonSerializerContext?.GetTypeInfo(typeof(TSignal));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TSignal>)jsonSerializerSettings.GetTypeInfo(typeof(TSignal));
        }

        return jsonTypeInfo;
    }
}
