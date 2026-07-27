namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.Middleware.Polly
internal class RetryMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required RetryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        var retryAttemptLimit = Configuration.RetryAttemptLimit;

        var usedRetryAttempts = 0;

        while (true)
        {
            try
            {
                return await ctx.Next(ctx.Message, ctx.CancellationToken);
            }
            catch when (usedRetryAttempts < retryAttemptLimit)
            {
                usedRetryAttempts += 1;
            }
        }
    }
}
