using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client code without an extra import)
namespace Conqueror;

internal static class ConquerorCommonMiddlewareAuthorizationAspNetApplicationBuilderExtensions
{
    /// <summary>
    ///     Add support for Conqueror authentication to the application. This middleware propagates the
    ///     ASP Core user (see <see cref="HttpContext.User" />) to the Conqueror authentication infrastructure.
    ///     It also handles Conqueror authentication failures and converts them to 401 responses.<br />
    ///     <br />
    ///     Note that this middleware MUST be placed after <see cref="AuthAppBuilderExtensions.UseAuthentication" />.
    /// </summary>
    public static IApplicationBuilder UseConquerorAuthentication(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            try
            {
                var authenticationContext = new ConquerorAuthenticationContext();

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
    public static IApplicationBuilder UseConquerorAuthorization(this IApplicationBuilder app)
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
