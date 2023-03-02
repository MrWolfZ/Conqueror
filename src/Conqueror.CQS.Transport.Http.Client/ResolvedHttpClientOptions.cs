using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client;

internal sealed class ResolvedHttpClientOptions
{
    public ResolvedHttpClientOptions(HttpClient httpClient,
                                     Uri baseAddress,
                                     JsonSerializerOptions? jsonSerializerOptions,
                                     IHttpCommandPathConvention? commandPathConvention,
                                     IHttpQueryPathConvention? queryPathConvention,
                                     HttpRequestHeaders? headers)
    {
        HttpClient = httpClient;
        BaseAddress = baseAddress;
        JsonSerializerOptions = jsonSerializerOptions;
        CommandPathConvention = commandPathConvention;
        QueryPathConvention = queryPathConvention;
        Headers = headers;
    }

    public HttpClient HttpClient { get; }

    public Uri BaseAddress { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; }

    public IHttpCommandPathConvention? CommandPathConvention { get; }

    public IHttpQueryPathConvention? QueryPathConvention { get; }

    public HttpRequestHeaders? Headers { get; }
}
