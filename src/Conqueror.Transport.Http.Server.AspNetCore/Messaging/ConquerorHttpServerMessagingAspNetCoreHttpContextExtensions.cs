using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "parameter is used to infer types")]
[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "parameter is used to infer types")]
public static class ConquerorHttpServerMessagingAspNetCoreHttpContextExtensions
{
    public static IMessageHandler<TMessage, TResponse> GetMessageClient<TMessage, TResponse>(this HttpContext httpContext)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        return httpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .For<TMessage, TResponse>()
                          .WithTransport(b => b.UseInProcessForHttpServer(httpContext));
    }

    public static IMessageHandler<TMessage, TResponse> GetMessageClient<TMessage, TResponse>(this HttpContext httpContext,
                                                                                             MessageTypes<TMessage, TResponse> message)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        return httpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .For<TMessage, TResponse>()
                          .WithTransport(b => b.UseInProcessForHttpServer(httpContext));
    }

    public static IMessageHandler<TMessage> GetMessageClient<TMessage>(this HttpContext httpContext)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
    {
        return httpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .For<TMessage>()
                          .WithTransport(b => b.UseInProcessForHttpServer(httpContext));
    }

    public static IMessageHandler<TMessage> GetMessageClient<TMessage>(this HttpContext httpContext,
                                                                       MessageTypes<TMessage, UnitMessageResponse> message)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
    {
        return httpContext.RequestServices
                          .GetRequiredService<IMessageClients>()
                          .For<TMessage>()
                          .WithTransport(b => b.UseInProcessForHttpServer(httpContext));
    }
}
