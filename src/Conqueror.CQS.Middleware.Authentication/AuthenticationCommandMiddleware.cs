using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authentication;

/// <summary>
///     A command middleware which adds authentication functionality to a command pipeline.
/// </summary>
public sealed class AuthenticationCommandMiddleware : ICommandMiddleware<AuthenticationCommandMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public AuthenticationCommandMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, AuthenticationCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        if (ctx.Configuration.RequireAuthenticatedPrincipal && authenticationContext.GetCurrentPrincipal() is null)
        {
            throw new ConquerorAuthenticationMissingPrincipalException($"command of type '{typeof(TCommand).Name}' requires an authenticated principal, but none was set");
        }

        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
