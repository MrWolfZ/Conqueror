using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalReceiver
{
    HttpSseSignalReceiverConfiguration Enable(Uri address);

    /// <summary>
    ///     Note that this is (usually) the service provider from the global scope,
    ///     and <i>not</i> the service provider from the scope of the send operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    void Disable();
}

public sealed class HttpSseSignalReceiverConfiguration
{
    public required Uri Address { get; init; }

    public HttpClient? HttpClient { get; private set; }

    public Action<HttpRequestHeaders>? ConfigureHeaders { get; private set; }

    public HttpSseSignalReceiverReconnectDelayFn? ReconnectDelayFn { get; private set; }

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

    public HttpSseSignalReceiverConfiguration WithReconnectDelayFunction(HttpSseSignalReceiverReconnectDelayFn retryDelayFn)
    {
        ReconnectDelayFn = retryDelayFn;

        return this;
    }
}

public delegate Task HttpSseSignalReceiverReconnectDelayFn(int statusCode, CancellationToken cancellationToken);
