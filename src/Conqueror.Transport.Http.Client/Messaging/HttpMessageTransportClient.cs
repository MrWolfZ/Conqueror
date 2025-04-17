using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.Messaging;

internal sealed class HttpMessageTransportClient<TMessage, TResponse>(Uri baseAddress)
    : IHttpMessageTransportClient<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    private readonly Lazy<HttpClient> defaultHttpClientSingletonLazy = new();

    private HttpClient? configuredHttpClient;
    private Action<HttpRequestHeaders> configureRequestHeaders = _ => { };

    public string TransportTypeName => ConquerorTransportHttpConstants.TransportName;

    public async Task<TResponse> Send(TMessage message,
                                      IServiceProvider serviceProvider,
                                      ConquerorContext conquerorContext,
                                      CancellationToken cancellationToken)
    {
        var httpClient = configuredHttpClient ?? defaultHttpClientSingletonLazy.Value;

        using var requestMessage = new HttpRequestMessage();
        requestMessage.Method = new(TMessage.HttpMethod);

        SetHeaders(conquerorContext, requestMessage.Headers);

        TracingHelper.SetTraceParentHeaderForTestClient(requestMessage.Headers, httpClient);

        var messageSerializer = TMessage.HttpMessageSerializer ?? HttpMessageBodySerializer<TMessage, TResponse>.Default;

        var (requestContent, path, queryString) = await messageSerializer.Serialize(serviceProvider, message, cancellationToken).ConfigureAwait(false);

        requestMessage.RequestUri = new(configuredHttpClient?.BaseAddress ?? baseAddress, (path ?? TMessage.FullPath) + (queryString ?? string.Empty));
        requestMessage.Content = requestContent;

        try
        {
            var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.BufferAndReadContent().ConfigureAwait(false);
                throw new HttpMessageFailedOnClientException($"HTTP message of type '{typeof(TMessage)}' failed with status code {response.StatusCode} and response content: {responseContent}")
                {
                    Response = response,
                    MessagePayload = message,
                    TransportType = new(TransportTypeName, MessageTransportRole.Client),
                };
            }

            ReadResponseHeaders(conquerorContext, response.Headers);

            var responseSerializer = TMessage.HttpResponseSerializer ?? HttpResponseBodySerializer<TMessage, TResponse>.Default;

            return await responseSerializer.Deserialize(serviceProvider, response.Content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not HttpMessageFailedOnClientException)
        {
            throw new HttpMessageFailedOnClientException($"HTTP message of type '{typeof(TMessage)}' failed", ex)
            {
                Response = null,
                MessagePayload = message,
                TransportType = new(TransportTypeName, MessageTransportRole.Client),
            };
        }
    }

    public IHttpMessageTransportClient<TMessage, TResponse> WithHttpClient(HttpClient? httpClient)
    {
        configuredHttpClient = httpClient;
        return this;
    }

    public IHttpMessageTransportClient<TMessage, TResponse> WithHeaders(Action<HttpRequestHeaders> configureHeaders)
    {
        var previousConfigureHeaders = configureRequestHeaders;
        configureRequestHeaders = h =>
        {
            previousConfigureHeaders(h);
            configureHeaders(h);
        };

        return this;
    }

    private void SetHeaders(ConquerorContext conquerorContext, HttpRequestHeaders headers)
    {
        if (Activity.Current is null && conquerorContext.GetTraceId() is { } traceId)
        {
            headers.Add(ConquerorTransportHttpConstants.TraceParentHeaderName, TracingHelper.CreateTraceParent(traceId: traceId));
        }

        if (conquerorContext.EncodeDownstreamContextData() is { } data)
        {
            headers.Add(ConquerorTransportHttpConstants.ConquerorContextHeaderName, data);
        }

        configureRequestHeaders(headers);
    }

    private static void ReadResponseHeaders(ConquerorContext conquerorContext, HttpResponseHeaders headers)
    {
        if (headers.TryGetValues(ConquerorTransportHttpConstants.ConquerorContextHeaderName, out var values))
        {
            conquerorContext.DecodeContextData(values);
        }
    }
}
