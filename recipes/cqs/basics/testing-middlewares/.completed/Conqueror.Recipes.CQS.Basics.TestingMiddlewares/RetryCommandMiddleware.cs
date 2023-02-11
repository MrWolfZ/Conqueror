namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares
{
    internal sealed class RetryCommandMiddleware : ICommandMiddleware<RetryMiddlewareConfiguration>
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, RetryMiddlewareConfiguration> ctx)
            where TCommand : class
        {
            var retryAttemptLimit = ctx.Configuration.RetryAttemptLimit;

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
}
