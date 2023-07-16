using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A query middleware which adds functional authorization functionality to a query pipeline.
/// </summary>
public sealed class FunctionalAuthorizationQueryMiddleware : IQueryMiddleware<FunctionalAuthorizationQueryMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public FunctionalAuthorizationQueryMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, FunctionalAuthorizationQueryMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        if (authenticationContext.GetCurrentPrincipal() is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var result = await ctx.Configuration.AuthorizationCheck(principal, typeof(TQuery)).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new ConquerorFunctionalAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute query '{typeof(TQuery).Name}'", result);
            }
        }

        return await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
    }
}
