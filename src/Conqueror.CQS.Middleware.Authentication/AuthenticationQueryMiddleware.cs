using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authentication;

/// <summary>
///     A query middleware which adds authentication functionality to a query pipeline.
/// </summary>
public sealed class AuthenticationQueryMiddleware : IQueryMiddleware<AuthenticationQueryMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public AuthenticationQueryMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, AuthenticationQueryMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        if (ctx.Configuration.RequireAuthenticatedPrincipal && authenticationContext.GetCurrentPrincipal() is null)
        {
            throw new ConquerorAuthenticationMissingPrincipalException($"query of type '{typeof(TQuery).Name}' requires an authenticated principal, but none was set");
        }

        return ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
