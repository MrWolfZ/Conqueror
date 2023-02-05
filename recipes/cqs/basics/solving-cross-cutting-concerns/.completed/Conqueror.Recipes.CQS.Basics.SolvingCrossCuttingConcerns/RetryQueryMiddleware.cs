namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal sealed class RetryQueryMiddleware : IQueryMiddleware<RetryMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, RetryMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        var retryAttemptLimit = ctx.Configuration.RetryAttemptLimit;

        var usedRetryAttempts = 0;

        while (true)
        {
            try
            {
                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
            catch
            {
                if (usedRetryAttempts >= retryAttemptLimit)
                {
                    throw;
                }

                usedRetryAttempts += 1;
            }
        }
    }
}
