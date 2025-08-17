namespace Conqueror;

internal sealed class HttpMessageBodyJsonSerializer<TMessage, TResponse> : IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public static readonly HttpMessageBodyJsonSerializer<TMessage, TResponse> Default = new();

    public string ContentType => MediaTypeNames.Application.Json;

    public Task SerializeMessageToBody(
        IServiceProvider serviceProvider,
        TMessage message,
        Stream bodyStream,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(bodyStream, message, GetJsonTypeInfo(serviceProvider), cancellationToken);

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, we are always disposing the stream"
    )]
    public async Task<TMessage> DeserializeMessage(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        Encoding? encoding,
        string path,
        IEnumerable<KeyValuePair<string, IReadOnlyList<string?>>> query,
        CancellationToken cancellationToken
    )
    {
        Stream? transcodingStream = null;

        try
        {
            if ((encoding?.Equals(Encoding.UTF8)) is false)
            {
                transcodingStream = Encoding.CreateTranscodingStream(
                    bodyStream,
                    encoding,
                    Encoding.UTF8,
                    leaveOpen: true
                );
            }

            var result = await JsonSerializer
                .DeserializeAsync(transcodingStream ?? bodyStream, GetJsonTypeInfo(serviceProvider), cancellationToken)
                .ConfigureAwait(false);

            return result
                ?? throw new IOException($"failed to deserialize HTTP body to message of type '{typeof(TMessage)}'");
        }
        finally
        {
            if (transcodingStream is not null)
            {
                await transcodingStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static JsonTypeInfo<TMessage> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TMessage>?)TMessage.HttpJsonSerializerContext?.GetTypeInfo(typeof(TMessage));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TMessage>)jsonSerializerSettings.GetTypeInfo(typeof(TMessage));
        }

        return jsonTypeInfo;
    }
}
