using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Polly;

/// <summary>
///     A query middleware which adds data annotation validation functionality to a query pipeline.
/// </summary>
public sealed class PollyQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public required PollyQueryMiddlewareConfiguration Configuration { get; init; }

    /// <inheritdoc />
    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        TResponse response = default!;

        await Configuration.Policy.ExecuteAsync(async () =>
        {
            // we cannot get the caller to give us a AsyncPolicy<TResponse>, so we need to capture
            // the response ourselves
            response = await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return response;
    }
}
