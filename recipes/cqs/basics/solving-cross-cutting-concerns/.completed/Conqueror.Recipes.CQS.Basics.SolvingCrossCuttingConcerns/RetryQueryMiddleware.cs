namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.CQS.Middleware.Polly
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
