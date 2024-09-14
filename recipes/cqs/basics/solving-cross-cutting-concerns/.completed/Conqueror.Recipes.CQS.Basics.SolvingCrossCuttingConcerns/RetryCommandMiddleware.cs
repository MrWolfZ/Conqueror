namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.CQS.Middleware.Polly
internal sealed class RetryCommandMiddleware : ICommandMiddleware
{
    public required RetryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        var retryAttemptLimit = Configuration.RetryAttemptLimit;

        var usedRetryAttempts = 0;

        while (true)
        {
            try
            {
                return await ctx.Next(ctx.Command, ctx.CancellationToken);
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
