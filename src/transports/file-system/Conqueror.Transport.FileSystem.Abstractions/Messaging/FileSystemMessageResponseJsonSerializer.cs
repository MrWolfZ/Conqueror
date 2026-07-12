namespace Conqueror;

internal sealed class FileSystemMessageResponseJsonSerializer<TMessage, TResponse>
    : IFileSystemMessageResponseSerializer<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    public static readonly FileSystemMessageResponseJsonSerializer<TMessage, TResponse> Default = new();

    public string FileExtension => ".json";

    public Task SerializeResponse(
        IServiceProvider serviceProvider,
        TResponse response,
        Stream fileStream,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(fileStream, response, GetJsonTypeInfo(serviceProvider), cancellationToken);

    public async Task<TResponse> DeserializeResponse(
        IServiceProvider serviceProvider,
        Stream fileStream,
        CancellationToken cancellationToken
    )
    {
        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return (TResponse)(object)UnitMessageResponse.Instance;
        }

        var result = await JsonSerializer
            .DeserializeAsync(fileStream, GetJsonTypeInfo(serviceProvider), cancellationToken)
            .ConfigureAwait(false);

        return result
               ?? throw new IOException(
                   $"failed to deserialize file stream to message response of type '{typeof(TResponse)}' (for message type '{typeof(TMessage)}')"
               );
    }

    private static JsonTypeInfo<TResponse> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TResponse>?)
            TMessage.FileSystemJsonSerializerContext?.GetTypeInfo(typeof(TResponse));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? FileSystemJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TResponse>)jsonSerializerSettings.GetTypeInfo(typeof(TResponse));
        }

        return jsonTypeInfo;
    }
}
