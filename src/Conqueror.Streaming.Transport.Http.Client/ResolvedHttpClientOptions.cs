using System;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class ResolvedHttpClientOptions
{
    public ResolvedHttpClientOptions(ConquerorStreamingWebSocketFactory socketFactory,
                                     Uri baseAddress,
                                     JsonSerializerOptions? jsonSerializerOptions,
                                     IHttpStreamPathConvention? pathConvention,
                                     HttpRequestHeaders? headers)
    {
        SocketFactory = socketFactory;
        BaseAddress = baseAddress;
        JsonSerializerOptions = jsonSerializerOptions;
        PathConvention = pathConvention;
        Headers = headers;
    }

    public ConquerorStreamingWebSocketFactory SocketFactory { get; }

    public Uri BaseAddress { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; }

    public IHttpStreamPathConvention? PathConvention { get; }

    public HttpRequestHeaders? Headers { get; }
}
