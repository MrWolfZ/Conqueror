using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.Polly
{
    /// <summary>
    ///     A query middleware which adds data annotation validation functionality to a query pipeline.
    /// </summary>
    public sealed class PollyQueryMiddleware : IQueryMiddleware<PollyQueryMiddlewareConfiguration>
    {
        /// <inheritdoc />
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, PollyQueryMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            TResponse response = default!;

            await ctx.Configuration.Policy.ExecuteAsync(async () =>
            {
                // we cannot get the caller to give us a AsyncPolicy<TResponse>, so we need to capture
                // the response ourselves
                response = await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return response;
        }
    }
}
