using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Conqueror.Eventing.Transport.WebSockets.Client;

internal sealed class ResolvedHttpClientOptions
{
    public ResolvedHttpClientOptions(HttpClient httpClient,
                                     Uri baseAddress,
                                     JsonSerializerOptions? jsonSerializerOptions,
                                     string? endpointPath,
                                     HttpRequestHeaders? headers)
    {
        HttpClient = httpClient;
        BaseAddress = baseAddress;
        JsonSerializerOptions = jsonSerializerOptions;
        EndpointPath = endpointPath;
        Headers = headers;
    }

    public HttpClient HttpClient { get; }

    public Uri BaseAddress { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; }

    public string? EndpointPath { get; }

    public HttpRequestHeaders? Headers { get; }
}
