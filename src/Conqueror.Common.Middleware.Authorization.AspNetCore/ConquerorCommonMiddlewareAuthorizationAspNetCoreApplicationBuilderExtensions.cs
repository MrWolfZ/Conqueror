using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.Common.Middleware.Authorization.AspNetCore;

public static class ConquerorCommonMiddlewareAuthorizationAspNetCoreApplicationBuilderExtensions
{
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
