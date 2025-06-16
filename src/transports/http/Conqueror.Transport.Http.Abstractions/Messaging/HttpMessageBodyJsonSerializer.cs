using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal sealed class HttpMessageBodyJsonSerializer<TMessage, TResponse>
    : IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public static readonly HttpMessageBodyJsonSerializer<TMessage, TResponse> Default = new();

    public string ContentType => MediaTypeNames.Application.Json;

    public Task SerializeToBody(
        IServiceProvider serviceProvider,
        TMessage message,
        Stream bodyStream,
        CancellationToken cancellationToken)
    {
        return JsonSerializer.SerializeAsync(
            bodyStream,
            message,
            GetJsonTypeInfo(serviceProvider),
            cancellationToken);
    }

    public async Task<TMessage> Deserialize(
        IServiceProvider serviceProvider,
        Stream bodyStream,
        Encoding? encoding,
        string path,
        IEnumerable<KeyValuePair<string, IReadOnlyList<string?>>> query,
        CancellationToken cancellationToken)
    {
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

            return result ?? throw new IOException($"failed to deserialize HTTP body to message of type '{typeof(TMessage)}'");
        }
        finally
        {
            if (transcodingStream != null)
            {
                await transcodingStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static JsonTypeInfo<TMessage> GetJsonTypeInfo(IServiceProvider serviceProvider)
    {
        var jsonTypeInfo = (JsonTypeInfo<TMessage>?)TMessage.HttpJsonSerializerContext?.GetTypeInfo(typeof(TMessage));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TMessage>)jsonSerializerSettings.GetTypeInfo(typeof(TMessage));
        }

        return jsonTypeInfo;
    }
}
