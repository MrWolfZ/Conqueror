using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

public static class HttpMessageServerTransportClientBuilderExtensions
{
    public static IMessageTransportClient<TMessage, TResponse> UseInProcessForHttpServer<TMessage, TResponse>(this IMessageTransportClientBuilder<TMessage, TResponse> builder,
                                                                                                              HttpContext httpContext)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

        return new HttpServerInProcessMessageTransport<TMessage, TResponse>(builder.UseInProcess(ConquerorTransportHttpConstants.TransportName), httpContext);
    }

    private sealed class HttpServerInProcessMessageTransport<TMessage, TResponse>(
        IMessageTransportClient<TMessage, TResponse> wrapped,
        HttpContext httpContext) : IMessageTransportClient<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public string TransportTypeName => ConquerorTransportHttpConstants.TransportName;

        public async Task<TResponse> Send(TMessage message, IServiceProvider serviceProvider, ConquerorContext conquerorContext, CancellationToken cancellationToken)
        {
            try
            {
                httpContext.PropagateConquerorContext(conquerorContext);
            }
            catch (FormattedConquerorContextDataInvalidException ex)
            {
                throw new MessageFailedDueToInvalidFormattedConquerorContextDataException<TMessage>(null, ex)
                {
                    MessagePayload = message,
                    TransportType = new(TransportTypeName, MessageTransportRole.Server),
                };
            }

            using var d = conquerorContext.SetCurrentPrincipalInternal(httpContext.User);

            return await wrapped.Send(message, serviceProvider, conquerorContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
