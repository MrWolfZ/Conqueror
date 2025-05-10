using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.Messaging;

internal sealed class HttpMessageResponseBodySerializer<TMessage, TResponse>
    : IHttpMessageResponseSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public static readonly HttpMessageResponseBodySerializer<TMessage, TResponse> Default = new();

    public string ContentType => MediaTypeNames.Application.Json;

    public Task Serialize(IServiceProvider serviceProvider, Stream body, TResponse response, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("not required for HTTP clients");
    }

    [UnconditionalSuppressMessage("Aot", "IL3050:RequiresDynamicCode", Justification = "testing")]
    [UnconditionalSuppressMessage("Aot", "IL2026:RequiresUnreferencedCode", Justification = "testing")]
    public async Task<TResponse> Deserialize(IServiceProvider serviceProvider, HttpContent content, CancellationToken cancellationToken)
    {
        if (typeof(TResponse) == typeof(UnitMessageResponse))
        {
            return (TResponse)(object)new UnitMessageResponse();
        }

        var jsonTypeInfo = (JsonTypeInfo<TResponse>?)TMessage.HttpJsonSerializerContext?.GetTypeInfo(typeof(TResponse));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TResponse>)jsonSerializerSettings.GetTypeInfo(typeof(TResponse));
        }

        return await content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(false)
               ?? throw new ArgumentException($"failed to deserialize HTTP message response of type '{typeof(TResponse).Name}'", nameof(content));
    }
}
