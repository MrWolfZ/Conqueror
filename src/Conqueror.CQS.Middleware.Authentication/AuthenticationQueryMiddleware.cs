using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authentication;

/// <summary>
///     A query middleware which adds authentication functionality to a query pipeline.
/// </summary>
public sealed class AuthenticationQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public AuthenticationQueryMiddlewareConfiguration Configuration { get; } = new();

    public Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        if (Configuration.RequireAuthenticatedPrincipal)
        {
            var currentPrincipal = ctx.ConquerorContext.GetCurrentPrincipal();

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
