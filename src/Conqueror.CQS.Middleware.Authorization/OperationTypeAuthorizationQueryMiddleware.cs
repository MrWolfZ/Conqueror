using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     A query middleware which adds operation type authorization functionality to a query pipeline.
/// </summary>
public sealed class OperationTypeAuthorizationQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public required OperationTypeAuthorizationQueryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        if (ctx.ConquerorContext.GetCurrentPrincipal() is { Identity: { IsAuthenticated: true } identity } principal)
        {
            var result = await Configuration.AuthorizationCheck(principal, typeof(TQuery)).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new ConquerorOperationTypeAuthorizationFailedException($"principal '{identity.Name}' is not authorized to execute query type '{typeof(TQuery).Name}'", result);
            }
        }

        return await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
    }
}
