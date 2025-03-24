using System;
using Conqueror.Transport.Http.Client.Messaging;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpMessageTransportClientBuilderExtensions
{
    public static IHttpMessageTransportClient<TMessage, TResponse> UseHttp<TMessage, TResponse>(
        this IMessageTransportClientBuilder<TMessage, TResponse> builder,
        Uri baseAddress)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));

        return new HttpMessageTransportClient<TMessage, TResponse>(baseAddress);
    }
}
