using System;
using System.Net.Http;
using System.Net.Http.Headers;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpMessageSender<in TMessage, TResponse> : IMessageSender<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    /// <summary>
    ///     Use the provided <see cref="HttpClient" /> to send the message.<br />
    ///     <br />
    ///     Note that if the HTTP client has a <see cref="HttpClient.BaseAddress" /> configured, it will be used instead
    ///     of the <see cref="Uri" /> that was provided when constructing the transport client originally.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use</param>
    /// <returns>The transport client configured to use the HTTP client</returns>
    IHttpMessageSender<TMessage, TResponse> WithHttpClient(HttpClient httpClient);

    IHttpMessageSender<TMessage, TResponse> WithHeaders(Action<HttpRequestHeaders> configureHeaders);
}
