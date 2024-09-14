using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authentication;

/// <summary>
///     A query middleware which adds authentication functionality to a query pipeline.
/// </summary>
public sealed class AuthenticationQueryMiddleware : IQueryMiddleware
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public AuthenticationQueryMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public AuthenticationQueryMiddlewareConfiguration Configuration { get; } = new();

    public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        if (Configuration.RequireAuthenticatedPrincipal)
        {
            var currentPrincipal = authenticationContext.CurrentPrincipal;

            if (currentPrincipal is null)
            {
                throw new ConquerorAuthenticationMissingPrincipalException($"query of type '{typeof(TQuery).Name}' requires an authenticated principal, but none was set");
            }

            if (!(currentPrincipal.Identity?.IsAuthenticated ?? false))
            {
                throw new ConquerorAuthenticationUnauthenticatedPrincipalException($"query type '{typeof(TQuery).Name}' requires an authenticated principal, but principal is not authenticated");
            }
        }

        return ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
