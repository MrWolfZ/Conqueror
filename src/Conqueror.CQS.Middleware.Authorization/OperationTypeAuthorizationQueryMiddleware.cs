using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A query middleware which adds operation type authorization functionality to a query pipeline.
/// </summary>
public sealed class OperationTypeAuthorizationQueryMiddleware : IQueryMiddleware<OperationTypeAuthorizationQueryMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public OperationTypeAuthorizationQueryMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, OperationTypeAuthorizationQueryMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var result = await ctx.Configuration.AuthorizationCheck(principal, typeof(TQuery)).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new ConquerorOperationTypeAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute query type '{typeof(TQuery).Name}'", result);
            }
        }

        return await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
    }
}
