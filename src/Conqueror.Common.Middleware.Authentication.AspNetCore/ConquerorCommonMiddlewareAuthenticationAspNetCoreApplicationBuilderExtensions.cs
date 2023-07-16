using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Common.Middleware.Authentication.AspNetCore;

public static class ConquerorCommonMiddlewareAuthenticationAspNetCoreApplicationBuilderExtensions
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
                var authenticationContext = ctx.RequestServices.GetRequiredService<IConquerorAuthenticationContext>();

                using var d = authenticationContext.SetCurrentPrincipal(ctx.User);

                await next().ConfigureAwait(false);
            }
            catch (ConquerorAuthenticationFailedException)
            {
                await ctx.ChallengeAsync().ConfigureAwait(false);
            }
        });
    }
}
