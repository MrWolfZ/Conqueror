using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
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
        CancellationToken cancellationToken)
    {
        return JsonSerializer.SerializeAsync(
            bodyStream,
            response,
            GetJsonTypeInfo(serviceProvider),
            cancellationToken);
    }

    public async Task<TResponse> DeserializeResponse(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        Encoding? encoding,
        CancellationToken cancellationToken)
    {
        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return (TResponse)(object)UnitMessageResponse.Instance;
        }

        Stream? transcodingStream = null;

        try
        {
            if (encoding != null && !encoding.Equals(Encoding.UTF8))
            {
                transcodingStream = Encoding.CreateTranscodingStream(
                    bodyStream,
                    encoding,
                    Encoding.UTF8,
                    leaveOpen: true);
            }

            var result = await JsonSerializer.DeserializeAsync(
                                                 transcodingStream ?? bodyStream,
                                                 GetJsonTypeInfo(serviceProvider),
                                                 cancellationToken)
                                             .ConfigureAwait(false);

            return result ?? throw new IOException($"failed to deserialize HTTP body to message response of type '{typeof(TResponse)}' (for message type '{typeof(TMessage)}')");
        }
        finally
        {
            if (transcodingStream != null)
            {
                await transcodingStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static JsonTypeInfo<TResponse> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TResponse>?)TMessage.HttpJsonSerializerContext?.GetTypeInfo(typeof(TResponse));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TResponse>)jsonSerializerSettings.GetTypeInfo(typeof(TResponse));
        }

        return jsonTypeInfo;
    }
}
