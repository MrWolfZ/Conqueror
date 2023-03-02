using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client;

public sealed class HttpCommandClientOptions
{
    private readonly Lazy<HttpRequestHeaders> headers = new(() =>
    {
        // HttpRequestHeaders does not have a public constructor, so we use this trick
        using var msg = new HttpRequestMessage();
        return msg.Headers;
    });

    internal HttpCommandClientOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public IHttpCommandPathConvention? PathConvention { get; set; }

    public HttpRequestHeaders Headers => headers.Value;

    internal HttpRequestHeaders? OptionalHeaders => headers.IsValueCreated ? headers.Value : null;
}
