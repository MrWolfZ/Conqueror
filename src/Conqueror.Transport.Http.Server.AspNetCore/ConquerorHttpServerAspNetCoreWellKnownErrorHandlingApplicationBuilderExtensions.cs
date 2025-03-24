using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client code without an extra import)
namespace Conqueror;

public static class ConquerorHttpServerAspNetCoreWellKnownErrorHandlingApplicationBuilderExtensions
{
    /// <summary>
    ///     Add support for handling well-known Conqueror errors to the application. This middleware handles
    ///     Conqueror well-known failures (e.g. authorization) and converts them to the appropriate responses
    ///     (e.g. 401 or 403).
    /// </summary>
    public static IApplicationBuilder UseConquerorWellKnownErrorHandling(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageFailedException ex) when (ex.Reason == MessageFailedException.WellKnownReasons.UnauthenticatedPrincipal)
            {
                await ctx.ChallengeAsync().ConfigureAwait(false);
            }
            catch (MessageFailedException ex) when (ex.Reason == MessageFailedException.WellKnownReasons.UnauthorizedPrincipal)
            {
                await ctx.ForbidAsync().ConfigureAwait(false);
            }
            catch (FormattedConquerorContextDataInvalidException) //// TODO: make this a well-known error
            {
                // using this instead of just writing the status code directly ensures that the request is properly logged
                await new BadRequestResult().ExecuteResultAsync(new() { HttpContext = ctx }).ConfigureAwait(false);

                // an invalid context could be a sign of malicious calls; therefore we don't
                // leak any details about what went wrong except that the context data was invalid
                await ctx.Response.WriteAsync("invalid conqueror context data").ConfigureAwait(false);
            }
        });
    }
}
