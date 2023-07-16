using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A query middleware which adds data authorization functionality to a query pipeline.
/// </summary>
public sealed class DataAuthorizationQueryMiddleware : IQueryMiddleware<DataAuthorizationQueryMiddlewareConfiguration>
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public DataAuthorizationQueryMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, DataAuthorizationQueryMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var results = await Task.WhenAll(ctx.Configuration.AuthorizationChecks.Select(c => c(principal, ctx.Query))).ConfigureAwait(false);
            var failures = results.Where(r => !r.IsSuccess).ToList();

            if (failures.Any())
            {
                var aggregatedFailure = failures.Count == 1 ? failures[0] : ConquerorAuthorizationResult.Failure(failures.SelectMany(f => f.FailureReasons).ToList());
                throw new ConquerorDataAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute query of type '{typeof(TQuery).Name}'", aggregatedFailure);
            }
        }

        return await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
    }
}
