using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "parameter is used to infer types")]
[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "parameter is used to infer types")]
public static class ConquerorHttpServerMessagingAspNetCoreHttpContextExtensions
{
    public static Task<TResponse> HandleMessage<TMessage, TResponse>(this HttpContext httpContext,
                                                                     IMessage<TMessage, TResponse> message)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        return httpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .ForMessageType<TMessage, TResponse>()
                          .WithTransport(b => b.UseInProcessForHttpServer(httpContext))
                          .Handle((TMessage)message, httpContext.RequestAborted);
    }

    public static Task HandleMessage<TMessage>(this HttpContext httpContext,
                                               IMessage<TMessage, UnitMessageResponse> message)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
    {
        return httpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .ForMessageType<TMessage>()
                          .WithTransport(b => b.UseInProcessForHttpServer(httpContext))
                          .Handle((TMessage)message, httpContext.RequestAborted);
    }
}
