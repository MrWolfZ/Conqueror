using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.Messaging;

internal sealed class HttpMessageSender<TMessage, TResponse>(Uri baseAddress)
    : IHttpMessageSender<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    private readonly Lazy<HttpClient> defaultHttpClientSingletonLazy = new();

    private HttpClient? configuredHttpClient;

    private Action<HttpRequestHeaders> configureRequestHeaders = _ =>
    {
    };

    private Version httpVersionField = HttpVersion.Version11;
    private HttpVersionPolicy httpVersionPolicyField = HttpVersionPolicy.RequestVersionOrLower;

    public string TransportTypeName => TransportName;

    public async Task<TResponse> Send(
        TMessage message,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        var httpClient = configuredHttpClient ?? defaultHttpClientSingletonLazy.Value;

        using var requestMessage = new HttpRequestMessage();

        requestMessage.Version = httpVersionField;
        requestMessage.VersionPolicy = httpVersionPolicyField;
        requestMessage.Method = new(TMessage.HttpMethod);

        SetHeaders(conquerorContext, requestMessage.Headers);

        TracingHelper.SetTraceParentHeaderForTestClient(requestMessage.Headers, httpClient);

        var messageSerializer = TMessage.HttpMessageSerializer;

        var path = messageSerializer.SerializeToPath(serviceProvider, message) ?? TMessage.FullPath;
        var queryString = messageSerializer.SerializeToQuery(serviceProvider, message) ?? string.Empty;

        requestMessage.RequestUri = new(configuredHttpClient?.BaseAddress ?? baseAddress, path + queryString);

        if (TMessage.EmptyInstance is null)
        {
            requestMessage.Content = new MessageContent(
                serviceProvider,
                message,
                messageSerializer,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(messageSerializer.ContentType))
            {
                requestMessage.Content.Headers.ContentType = new(messageSerializer.ContentType);
            }
        }

        try
        {
            var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.BufferAndReadContent().ConfigureAwait(false);

                throw new HttpMessageFailedOnClientException(
                    $"HTTP message of type '{typeof(TMessage)}' failed with status code {response.StatusCode} and response content: {responseContent}")
                {
                    Response = response,
                    MessagePayload = message,
                    TransportType = new(TransportTypeName, MessageTransportRole.Sender),
                };
            }

            ReadResponseHeaders(conquerorContext, response.Headers);

            var mediaType = response.Content.Headers.ContentType;

            if (typeof(TResponse) == typeof(UnitMessageResponse))
            {
                if (mediaType?.MediaType is not null)
                {
                    throw new HttpMessageFailedOnClientException(
                        $"HTTP message of type '{typeof(TMessage)}' failed due to mismatching content type; expected no response, got '{mediaType.MediaType}'")
                    {
                        Response = response,
                        MessagePayload = message,
                        TransportType = new(TransportTypeName, MessageTransportRole.Sender),
                    };
                }

                return (TResponse)(object)UnitMessageResponse.Instance;
            }

            if (mediaType?.MediaType != TMessage.HttpMessageResponseSerializer.ContentType)
            {
                throw new HttpMessageFailedOnClientException(
                    $"HTTP message of type '{typeof(TMessage)}' failed due to mismatching content type; expected '{TMessage.HttpMessageResponseSerializer.ContentType}', got '{mediaType?.MediaType}'")
                {
                    Response = response,
                    MessagePayload = message,
                    TransportType = new(TransportTypeName, MessageTransportRole.Sender),
                };
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var responseSerializer = TMessage.HttpMessageResponseSerializer;

            var encoding = string.IsNullOrWhiteSpace(mediaType.CharSet) ? null : Encoding.GetEncoding(mediaType.CharSet);
            return await responseSerializer.Deserialize(serviceProvider, responseStream, encoding, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not HttpMessageFailedOnClientException)
        {
            throw new HttpMessageFailedOnClientException($"HTTP message of type '{typeof(TMessage)}' failed", ex)
            {
                Response = null,
                MessagePayload = message,
                TransportType = new(TransportTypeName, MessageTransportRole.Sender),
            };
        }
    }

    public IHttpMessageSender<TMessage, TResponse> WithHttpClient(HttpClient httpClient)
    {
        configuredHttpClient = httpClient;

        return this;
    }

    public IHttpMessageSender<TMessage, TResponse> WithHeaders(Action<HttpRequestHeaders> configureHeaders)
    {
        var previousConfigureHeaders = configureRequestHeaders;
        configureRequestHeaders = h =>
        {
            previousConfigureHeaders(h);
            configureHeaders(h);
        };

        return this;
    }

    public IHttpMessageSender<TMessage, TResponse> WithHttpVersion(Version httpVersion, HttpVersionPolicy versionPolicy)
    {
        httpVersionField = httpVersion;
        httpVersionPolicyField = versionPolicy;

        return this;
    }

    private void SetHeaders(ConquerorContext conquerorContext, HttpRequestHeaders headers)
    {
        // since we send the trace ID already separately, we don't need to include it in the context data
        var traceId = conquerorContext.RemoveTraceId();

        if (Activity.Current is null)
        {
            headers.Add(HeaderNames.TraceParent, TracingHelper.CreateTraceParent(traceId: traceId));
        }

        if (conquerorContext.EncodeDownstreamContextData() is { } data)
        {
            headers.Add(HeaderNames.ConquerorContext, data);
        }

        configureRequestHeaders(headers);
    }

    private static void ReadResponseHeaders(ConquerorContext conquerorContext, HttpResponseHeaders headers)
    {
        if (headers.TryGetValues(HeaderNames.ConquerorContext, out var values))
        {
            conquerorContext.DecodeContextData(values);
        }
    }

    private sealed class MessageContent(
        IServiceProvider serviceProvider,
        TMessage message,
        IHttpMessageSerializer<TMessage, TResponse> serializer,
        CancellationToken cancellationToken) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => serializer.SerializeToBody(
                serviceProvider,
                message,
                stream,
                cancellationToken);

        protected override bool TryComputeLength(out long length)
            => serializer.TryGetBodyLength(serviceProvider, message, out length);
    }
}
