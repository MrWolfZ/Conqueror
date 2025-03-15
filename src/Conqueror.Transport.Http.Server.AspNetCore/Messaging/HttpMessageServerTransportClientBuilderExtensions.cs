using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

public static class HttpMessageServerTransportClientBuilderExtensions
{
    public static IMessageTransportClient<TMessage, TResponse> UseInProcessForHttpServer<TMessage, TResponse>(this IMessageTransportClientBuilder<TMessage, TResponse> builder,
                                                                                                              HttpContext httpContext)
        where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
    {
        httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

        return new HttpServerInProcessMessageTransport<TMessage, TResponse>(builder.UseInProcess(ConquerorTransportHttpConstants.TransportName), httpContext);
    }

    private sealed class HttpServerInProcessMessageTransport<TMessage, TResponse>(
        IMessageTransportClient<TMessage, TResponse> wrapped,
        HttpContext httpContext) : IMessageTransportClient<TMessage, TResponse>
        where TMessage : class, IMessage<TResponse>
    {
        public string TransportTypeName => ConquerorTransportHttpConstants.TransportName;

        public Task<TResponse> Send(TMessage message, IServiceProvider serviceProvider, ConquerorContext conquerorContext, CancellationToken cancellationToken)
        {
            httpContext.PropagateConquerorContext(conquerorContext);

            return wrapped.Send(message, serviceProvider, conquerorContext, cancellationToken);
        }
    }
}
