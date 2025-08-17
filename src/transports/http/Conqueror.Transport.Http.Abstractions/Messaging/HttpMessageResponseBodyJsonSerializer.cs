namespace Conqueror;

internal sealed class HttpMessageResponseBodyJsonSerializer<TMessage, TResponse>
    : IHttpMessageResponseSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public static readonly HttpMessageResponseBodyJsonSerializer<TMessage, TResponse> Default = new();

    public string ContentType => MediaTypeNames.Application.Json;

    public Task SerializeResponse(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        TResponse response,
        CancellationToken cancellationToken
    ) => JsonSerializer.SerializeAsync(bodyStream, response, GetJsonTypeInfo(serviceProvider), cancellationToken);

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, we are always disposing the stream"
    )]
    public async Task<TResponse> DeserializeResponse(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        Encoding? encoding,
        CancellationToken cancellationToken
    )
    {
        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return (TResponse)(object)UnitMessageResponse.Instance;
        }

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
                ?? throw new IOException(
                    $"failed to deserialize HTTP body to message response of type '{typeof(TResponse)}' (for message type '{typeof(TMessage)}')"
                );
        }
        finally
        {
            if (transcodingStream is not null)
            {
                await transcodingStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static JsonTypeInfo<TResponse> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TResponse>?)TMessage.HttpJsonSerializerContext?.GetTypeInfo(typeof(TResponse));

        if (jsonTypeInfo is null)
        {
            var jsonSerializerSettings =
                (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TResponse>)jsonSerializerSettings.GetTypeInfo(typeof(TResponse));
        }

        return jsonTypeInfo;
    }
}
