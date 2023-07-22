using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client code without an extra import)
namespace Conqueror;

public static class ConquerorCommonMiddlewareAuthorizationAspNet
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
        return app.UseConquerorAuthentication()
                  .UseConquerorAuthorization();
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
