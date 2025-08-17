#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client code without an extra import

namespace Conqueror;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Streaming.Transport.Http.Server.AspNetCore;

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
    /// <param name="app">The app to configure</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseConqueror(this IApplicationBuilder app) =>
        app.UseConquerorContextDataPropagation();

    /// <summary>
    ///     Add support for Conqueror context data propagation to the application.
    /// </summary>
    /// <param name="app">The app to configure</param>
    private static IApplicationBuilder UseConquerorContextDataPropagation(this IApplicationBuilder app)
    {
        return app.Use(
            async (httpContext, next) =>
            {
                try
                {
                    var conquerorContextAccessor =
                        httpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>();
                    using var conquerorContext = conquerorContextAccessor.GetOrCreate();

                    ReadContextDataFromRequest(conquerorContext, httpContext);

                    ConquerorServerTransportHelper.SignalExecution(conquerorContext, HttpConstants.TransportName);

                    ConquerorServerTransportHelper.HandleTraceParent(conquerorContext, GetTraceParent(httpContext));

                    // ReSharper disable once AccessToDisposedClosure (accessing the disposed context is fine, since disposing it only clears it from the async local)
                    httpContext.Response.OnStarting(
                        state => SetResponseHeaders((HttpContext)state, conquerorContext),
                        httpContext
                    );

                    await next().ConfigureAwait(false);
                }
                catch (FormattedConquerorContextDataInvalidException)
                {
                    await new BadRequestResult()
                        .ExecuteResultAsync(new() { HttpContext = httpContext })
                        .ConfigureAwait(false);

                    // an invalid context could be a sign of malicious calls; therefore we don't
                    // leak any details about what went wrong except that the context data was invalid
                    await httpContext
                        .Response.WriteAsync("invalid conqueror context data", CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
        );

        static void ReadContextDataFromRequest(ConquerorContext ctx, HttpContext httpContext)
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

        static Task SetResponseHeaders(HttpContext httpContext, ConquerorContext conquerorContext)
        {
            if (conquerorContext.EncodeUpstreamContextData() is { } data)
            {
                httpContext.Response.Headers[HttpConstants.ConquerorContextHeaderName] = data;
            }

            return Task.CompletedTask;
        }
    }
}
