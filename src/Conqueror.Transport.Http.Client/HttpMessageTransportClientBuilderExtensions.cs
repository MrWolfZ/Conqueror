using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpMessageTransportClientBuilderExtensions
{
    public static IMessageTransportClient UseHttp<TMessage, TResponse>(this IMessageTransportClientBuilder<TMessage, TResponse> builder,
                                                                       Uri baseAddress,
                                                                       HttpClient? httpClient = null,
                                                                       Action<HttpRequestHeaders>? configureHeaders = null,
                                                                       JsonSerializerOptions? jsonSerializerOptions = null)
        where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage>
    {
        baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));

        _ = baseAddress;

        throw new NotImplementedException();
    }
}
