namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.CQS.Middleware.Polly
internal sealed class RetryCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public required RetryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
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
