using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authentication;

/// <summary>
///     A command middleware which adds authentication functionality to a command pipeline.
/// </summary>
public sealed class AuthenticationCommandMiddleware : ICommandMiddleware
{
    public AuthenticationCommandMiddlewareConfiguration Configuration { get; } = new();

    public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        if (Configuration.RequireAuthenticatedPrincipal)
        {
            var authenticationContext = new ConquerorAuthenticationContext();
            var currentPrincipal = authenticationContext.CurrentPrincipal;

            if (currentPrincipal is null)
            {
                throw new ConquerorAuthenticationMissingPrincipalException($"command of type '{typeof(TCommand).Name}' requires an authenticated principal, but none was set");
            }

            if (!(currentPrincipal.Identity?.IsAuthenticated ?? false))
            {
                throw new ConquerorAuthenticationUnauthenticatedPrincipalException($"command of type '{typeof(TCommand).Name}' requires an authenticated principal, but principal is not authenticated");
            }
        }

        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
