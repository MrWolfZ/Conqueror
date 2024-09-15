using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client;

internal sealed class ResolvedHttpClientOptions(
    HttpClient httpClient,
    Uri baseAddress,
    JsonSerializerOptions? jsonSerializerOptions,
    IHttpCommandPathConvention? commandPathConvention,
    IHttpQueryPathConvention? queryPathConvention,
    HttpRequestHeaders? headers)
{
    public HttpClient HttpClient { get; } = httpClient;

    public Uri BaseAddress { get; } = baseAddress;

    public JsonSerializerOptions? JsonSerializerOptions { get; } = jsonSerializerOptions;

    public IHttpCommandPathConvention? CommandPathConvention { get; } = commandPathConvention;

    public IHttpQueryPathConvention? QueryPathConvention { get; } = queryPathConvention;

    public HttpRequestHeaders? Headers { get; } = headers;
}
