﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Conqueror.Transport.Http.Server.AspNetCore;

internal static class ConquerorHttpServerAspNetCoreHttpContextExtensions
{
    /// <summary>
    ///     Add support for Conqueror context data propagation to the application.
    /// </summary>
    public static void PropagateConquerorContext(this HttpContext httpContext, ConquerorContext conquerorContext)
    {
        ReadContextDataFromRequest(conquerorContext, httpContext);

        ConquerorServerTransportHelper.HandleTraceParent(conquerorContext, GetTraceParent(httpContext));

        // ReSharper disable once AccessToDisposedClosure (accessing the disposed context is fine, since disposing it only clears it from the async local)
        httpContext.Response.OnStarting(state => SetResponseHeaders((HttpContext)state, conquerorContext), httpContext);

        static void ReadContextDataFromRequest(ConquerorContext ctx, HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(ConquerorTransportHttpConstants.ConquerorContextHeaderName, out var values))
            {
                ctx.DecodeContextData(values as IEnumerable<string>);
            }
        }

        static string? GetTraceParent(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues))
            {
                return traceParentValues.FirstOrDefault();
            }

            return null;
        }

        static Task SetResponseHeaders(HttpContext httpContext, ConquerorContext conquerorContext)
        {
            if (conquerorContext.EncodeUpstreamContextData() is { } data)
            {
                httpContext.Response.Headers[ConquerorTransportHttpConstants.ConquerorContextHeaderName] = data;
            }

            return Task.CompletedTask;
        }
    }
}
