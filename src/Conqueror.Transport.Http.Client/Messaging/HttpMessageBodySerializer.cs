using System;
using System.Collections.Generic;
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

internal sealed class HttpMessageBodySerializer<TMessage, TResponse>
    : IHttpMessageSerializer<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public static readonly HttpMessageBodySerializer<TMessage, TResponse> Default = new();

    public string ContentType => MediaTypeNames.Application.Json;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "we are returning the content, and it is disposed at the call site")]
    public Task<(HttpContent? Content, string? Path, string? QueryString)> Serialize(IServiceProvider serviceProvider, TMessage message, CancellationToken cancellationToken)
    {
        var jsonTypeInfo = (JsonTypeInfo<TMessage>?)TMessage.JsonSerializerContext?.GetTypeInfo(typeof(TMessage));

        if (jsonTypeInfo == null)
        {
            var jsonSerializerSettings = (JsonSerializerOptions?)serviceProvider.GetService(typeof(JsonSerializerOptions))
                                         ?? HttpJsonSerializerOptions.DefaultJsonSerializerOptions;
            jsonTypeInfo = (JsonTypeInfo<TMessage>)jsonSerializerSettings.GetTypeInfo(typeof(TMessage));
        }

        var content = JsonContent.Create(message, jsonTypeInfo, new(ContentType));

        return Task.FromResult<(HttpContent? Content, string? Path, string? QueryString)>((content, null, null));
    }

    public Task<TMessage> Deserialize(IServiceProvider serviceProvider,
                                      Stream body,
                                      string path,
                                      IReadOnlyDictionary<string, IReadOnlyList<string?>>? query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("not required for HTTP clients");
    }
}
