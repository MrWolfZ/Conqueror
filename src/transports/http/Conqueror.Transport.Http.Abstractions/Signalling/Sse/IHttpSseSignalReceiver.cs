using System;
using System.Net.Http;
using System.Net.Http.Headers;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalReceiver : ISignalReceiver<IHttpSseSignalReceiver>
{
    HttpSseSignalReceiverConfiguration Enable(Uri address);
}

public sealed class HttpSseSignalReceiverConfiguration
{
    public required string EventType { get; init; }

    public required Uri Address { get; init; }

    public HttpClient? HttpClient { get; private set; }

    public Action<HttpRequestHeaders>? ConfigureHeaders { get; private set; }

    /// <summary>
    ///     Use the provided <see cref="HttpClient" /> to connect to the SEE endpoint.<br />
    ///     <br />
    ///     Note that if the HTTP client has a <see cref="HttpClient.BaseAddress" /> configured, it will be used instead
    ///     of the <see cref="Uri" /> that was provided when constructing the transport client originally.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use</param>
    /// <returns>The transport client configured to use the HTTP client</returns>
    public HttpSseSignalReceiverConfiguration WithHttpClient(HttpClient httpClient)
    {
        HttpClient = httpClient;

        return this;
    }

    public HttpSseSignalReceiverConfiguration WithHeaders(Action<HttpRequestHeaders> configureHeaders)
    {
        ConfigureHeaders = configureHeaders;

        return this;
    }
}
