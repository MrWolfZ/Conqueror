using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A query middleware which adds payload authorization functionality to a query pipeline.
/// </summary>
public sealed class PayloadAuthorizationQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    private readonly IConquerorAuthenticationContext authenticationContext;

    public PayloadAuthorizationQueryMiddleware(IConquerorAuthenticationContext authenticationContext)
    {
        this.authenticationContext = authenticationContext;
    }

    public PayloadAuthorizationQueryMiddlewareConfiguration<TQuery> Configuration { get; } = new();

    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        if (authenticationContext.CurrentPrincipal is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var results = await Task.WhenAll(Configuration.AuthorizationChecks.Select(c => c(principal, ctx.Query))).ConfigureAwait(false);
            var failures = results.Where(r => !r.IsSuccess).ToList();

            if (failures.Any())
            {
                var aggregatedFailure = failures.Count == 1 ? failures[0] : ConquerorAuthorizationResult.Failure(failures.SelectMany(f => f.FailureReasons).ToList());
                throw new ConquerorOperationPayloadAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute query of type '{typeof(TQuery).Name}'", aggregatedFailure);
            }
        }

        return await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
    }
}
