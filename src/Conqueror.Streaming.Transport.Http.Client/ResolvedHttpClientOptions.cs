using System;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class ResolvedHttpClientOptions(
    ConquerorStreamingWebSocketFactory socketFactory,
    Uri baseAddress,
    JsonSerializerOptions? jsonSerializerOptions,
    IHttpStreamPathConvention? pathConvention,
    HttpRequestHeaders? headers)
{
    public ConquerorStreamingWebSocketFactory SocketFactory { get; } = socketFactory;

    public Uri BaseAddress { get; } = baseAddress;

    public JsonSerializerOptions? JsonSerializerOptions { get; } = jsonSerializerOptions;

    public IHttpStreamPathConvention? PathConvention { get; } = pathConvention;

    public HttpRequestHeaders? Headers { get; } = headers;
}
