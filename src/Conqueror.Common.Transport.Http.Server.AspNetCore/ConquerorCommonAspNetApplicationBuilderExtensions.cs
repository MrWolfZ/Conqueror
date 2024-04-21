using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client code without an extra import)
namespace Conqueror;

public static class ConquerorCommonAspNetApplicationBuilderExtensions
{
    /// <summary>
    ///     Add support for Conqueror to the ASP.NET Core pipeline. This middleware includes various other
    ///     middlewares that each support different features of Conqueror.<br />
    ///     <br />
    ///     Please note that this middleware should be placed as late as possible in the pipeline (i.e. just
    ///     before adding endpoints) and MUST be placed after <see cref="AuthAppBuilderExtensions.UseAuthentication" />
    ///     (if you are using that middleware).
    /// </summary>
    public static IApplicationBuilder UseConqueror(this IApplicationBuilder app)
    {
        return app.UseConquerorContextDataPropagation()
                  .UseConquerorAuthentication()
                  .UseConquerorAuthorization();
    }

    /// <summary>
    ///     Add support for Conqueror context data propagation to the application.
    /// </summary>
    private static IApplicationBuilder UseConquerorContextDataPropagation(this IApplicationBuilder app)
    {
        return app.Use(async (httpContext, next) =>
        {
            try
            {
                var conquerorContextAccessor = httpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>();
                using var conquerorContext = conquerorContextAccessor.GetOrCreate();

                ReadContextDataFromRequest(conquerorContext, httpContext);

                ConquerorServerTransportHelper.SignalExecution(conquerorContext);

                ConquerorServerTransportHelper.HandleTraceParent(conquerorContext, GetTraceParent(httpContext));

                // ReSharper disable once AccessToDisposedClosure (accessing the disposed context is fine, since disposing it only clears it from the async local)
                httpContext.Response.OnStarting(state => SetResponseHeaders((HttpContext)state, conquerorContext), httpContext);

                await next().ConfigureAwait(false);
            }
            catch (FormattedConquerorContextDataInvalidException)
            {
                await new BadRequestResult().ExecuteResultAsync(new() { HttpContext = httpContext }).ConfigureAwait(false);

                // an invalid context could be a sign of malicious calls; therefore we don't
                // leak any details about what went wrong except that the context data was invalid
                await httpContext.Response.WriteAsync("invalid conqueror context data").ConfigureAwait(false);
            }
        });

        static void ReadContextDataFromRequest(IConquerorContext ctx, HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values))
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

        static Task SetResponseHeaders(HttpContext httpContext, IConquerorContext conquerorContext)
        {
            if (conquerorContext.EncodeUpstreamContextData() is { } data)
            {
                httpContext.Response.Headers[HttpConstants.ConquerorContextHeaderName] = data;
            }

            return Task.CompletedTask;
        }
    }
}
