using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "parameter is used to infer types")]
[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "parameter is used to infer types")]
public static class ConquerorHttpServerMessagingAspNetCoreHttpContextExtensions
{
    public static async Task<TResponse> HandleMessage<TMessage, TResponse>(this HttpContext httpContext, IHttpMessage<TMessage, TResponse> message)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
    {
        var invoker = httpContext.RequestServices.GetRequiredService<IMessageHandlerRegistry>()
                                 .GetReceiverHandlerInvoker<TMessage, TResponse, IHttpMessageHandlerTypesInjector>();

        if (invoker is null)
        {
            throw new InvalidOperationException($"no handler is registered for HTTP message type '{typeof(TMessage)}'");
        }

        using var conquerorContext = httpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        try
        {
            conquerorContext.DecodeContextData(ReadContextDataFromRequest(httpContext));
        }
        catch (FormattedConquerorContextDataInvalidException ex)
        {
            throw new MessageFailedDueToInvalidFormattedConquerorContextDataException($"badly formatted context data while processing HTTP message of type '{typeof(TMessage)}'", ex)
            {
                MessagePayload = message,
                TransportType = new(ConquerorTransportHttpConstants.TransportName, MessageTransportRole.Receiver),
            };
        }

        if (GetTraceId(httpContext) is { } traceId)
        {
            conquerorContext.SetTraceId(traceId);
        }

        using var principal = conquerorContext.SetCurrentPrincipalInternal(httpContext.User);

        var response = await invoker.Invoke<TMessage, TResponse>((TMessage)message,
                                                                 httpContext.RequestServices,
                                                                 ConquerorTransportHttpConstants.TransportName,
                                                                 httpContext.RequestAborted)
                                    .ConfigureAwait(false);

        httpContext.Response.StatusCode = TMessage.SuccessStatusCode;

        if (conquerorContext.EncodeUpstreamContextData() is { } data)
        {
            httpContext.Response.Headers[ConquerorTransportHttpConstants.ConquerorContextHeaderName] = data;
        }

        return response;

        static IEnumerable<string> ReadContextDataFromRequest(HttpContext httpContext)
            => httpContext.Request.Headers.TryGetValue(ConquerorTransportHttpConstants.ConquerorContextHeaderName, out var values) ? values : [];

        static string? GetTraceId(HttpContext httpContext)
        {
            string? traceParent = null;

            if (httpContext.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues))
            {
                traceParent = traceParentValues.FirstOrDefault();
            }

            if (Activity.Current is null && traceParent is not null)
            {
                using var a = new Activity(string.Empty);
                return a.SetParentId(traceParent).TraceId.ToString();
            }

            return null;
        }
    }

    public static Task HandleMessage<TMessage>(this HttpContext httpContext, TMessage message)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        => HandleMessage<TMessage, UnitMessageResponse>(httpContext, message);
}
