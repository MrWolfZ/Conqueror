using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Common.Middleware.Authentication.AspNetCore;

public static class ConquerorCommonMiddlewareAuthenticationAspNetCoreApplicationBuilderExtensions
{
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
            catch (ConquerorAuthenticationMissingPrincipalException)
            {
                await ctx.ChallengeAsync().ConfigureAwait(false);
            }
            catch (ConquerorAuthenticationUnauthenticatedPrincipalException)
            {
                await ctx.ChallengeAsync().ConfigureAwait(false);
            }
        });
    }
}
