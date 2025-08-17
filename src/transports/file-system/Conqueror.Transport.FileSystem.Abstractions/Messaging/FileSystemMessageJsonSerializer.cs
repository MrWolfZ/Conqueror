namespace Conqueror;

internal sealed class FileSystemMessageJsonSerializer<TMessage, TResponse>
    : IFileSystemMessageSerializer<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    public static readonly FileSystemMessageJsonSerializer<TMessage, TResponse> Default = new();

    public string FileExtension => ".json";

    public Task SerializeMessage(
        IServiceProvider serviceProvider,
        TMessage message,
        Stream fileStream,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(fileStream, message, GetJsonTypeInfo(serviceProvider), cancellationToken);

    public async Task<TMessage> DeserializeMessage(
        IServiceProvider serviceProvider,
        Stream fileStream,
        CancellationToken cancellationToken
    )
    {
        var result = await JsonSerializer
            .DeserializeAsync(fileStream, GetJsonTypeInfo(serviceProvider), cancellationToken)
            .ConfigureAwait(false);

        return result
            ?? throw new IOException($"failed to deserialize file stream to message of type '{typeof(TMessage)}'");
    }

    private static JsonTypeInfo<TMessage> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TMessage>?)
            TMessage.FileSystemJsonSerializerContext?.GetTypeInfo(typeof(TMessage));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? FileSystemJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TMessage>)jsonSerializerSettings.GetTypeInfo(typeof(TMessage));
        }

        return jsonTypeInfo;
    }
}
