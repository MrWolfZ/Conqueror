using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client code without an extra import)
namespace Conqueror;

public static class ConquerorCommonMiddlewareAuthorizationAspNetApplicationBuilderExtensions
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
        return app.Use(async (ctx, next) =>
        {
            try
            {
                var conquerorContextAccessor = ctx.RequestServices.GetRequiredService<IConquerorContextAccessor>();
                using var conquerorContext = conquerorContextAccessor.GetOrCreate();

                ConquerorServerTransportHelper.ReadContextData(conquerorContext, GetFormattedDownstreamContextData(ctx), GetFormattedContextData(ctx));

                ConquerorServerTransportHelper.SignalExecution(conquerorContext);

                ConquerorServerTransportHelper.HandleTraceParent(conquerorContext, GetTraceParent(ctx));

                // ReSharper disable once AccessToDisposedClosure (accessing the disposed context is fine, since disposing it only clears it from the async local)
                ctx.Response.OnStarting(state => SetResponseHeaders((HttpContext)state, conquerorContext), ctx);

                await next().ConfigureAwait(false);
            }
            catch (FormattedConquerorContextDataInvalidException)
            {
                await new BadRequestResult().ExecuteResultAsync(new() { HttpContext = ctx }).ConfigureAwait(false);

                // an invalid context could be a sign of malicious calls; therefore we don't
                // leak any details about what went wrong except that the context data was invalid
                await ctx.Response.WriteAsync("invalid conqueror context data").ConfigureAwait(false);
            }
        });

        static IEnumerable<string> GetFormattedDownstreamContextData(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorDownstreamContextHeaderName, out var values))
            {
                return values;
            }

            return Enumerable.Empty<string>();
        }

        static IEnumerable<string> GetFormattedContextData(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                return values;
            }

            return Enumerable.Empty<string>();
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
            if (ConquerorContextDataFormatter.Format(conquerorContext.UpstreamContextData) is { } formattedUpstreamData)
            {
                httpContext.Response.Headers[HttpConstants.ConquerorUpstreamContextHeaderName] = formattedUpstreamData;
            }

            if (ConquerorContextDataFormatter.Format(conquerorContext.ContextData) is { } formattedData)
            {
                httpContext.Response.Headers[HttpConstants.ConquerorContextHeaderName] = formattedData;
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Add support for Conqueror authentication to the application. This middleware propagates the
    ///     ASP Core user (see <see cref="HttpContext.User" />) to the Conqueror authentication infrastructure.
    ///     It also handles Conqueror authentication failures and converts them to 401 responses.<br />
    ///     <br />
    ///     Note that this middleware MUST be placed after <see cref="AuthAppBuilderExtensions.UseAuthentication" />.
    /// </summary>
    private static IApplicationBuilder UseConquerorAuthentication(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            try
            {
                var authenticationContext = ctx.RequestServices.GetService<IConquerorAuthenticationContext>();

                // the support for authentication is optional, so we simply skip this middleware if the
                // authentication context is not available
                if (authenticationContext is null)
                {
                    await next().ConfigureAwait(false);
                    return;
                }

                using var d = authenticationContext.SetCurrentPrincipal(ctx.User);

                await next().ConfigureAwait(false);
            }
            catch (ConquerorAuthenticationFailedException)
            {
                await ctx.ChallengeAsync().ConfigureAwait(false);
            }
        });
    }

    /// <summary>
    ///     Add support for Conqueror authorization to the application. This middleware handles
    ///     Conqueror authorization failures and converts them to 403 responses.
    /// </summary>
    private static IApplicationBuilder UseConquerorAuthorization(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (ConquerorAuthorizationFailedException)
            {
                await ctx.ForbidAsync().ConfigureAwait(false);
            }
        });
    }
}
