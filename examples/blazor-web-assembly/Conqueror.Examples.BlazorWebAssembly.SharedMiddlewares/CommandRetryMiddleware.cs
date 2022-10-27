namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record CommandRetryMiddlewareConfiguration(int MaxNumberOfAttempts, TimeSpan RetryInterval);

public sealed class CommandRetryMiddleware : ICommandMiddleware<CommandRetryMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandRetryMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class RetryCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseRetry(this ICommandPipelineBuilder pipeline,
                                                   int maxNumberOfAttempts = 3,
                                                   TimeSpan? retryInterval = null)
    {
        var configuration = new CommandRetryMiddlewareConfiguration(maxNumberOfAttempts, retryInterval ?? TimeSpan.FromSeconds(1));

        return pipeline.Use<CommandRetryMiddleware, CommandRetryMiddlewareConfiguration>(configuration);
    }
    
    public static ICommandPipelineBuilder ConfigureRetry(this ICommandPipelineBuilder pipeline,
                                                         int? maxNumberOfAttempts = null,
                                                         TimeSpan? retryInterval = null)
    {
        return pipeline.Configure<CommandRetryMiddleware, CommandRetryMiddlewareConfiguration>(c =>
        {
            if (maxNumberOfAttempts is not null)
            {
                c = c with { MaxNumberOfAttempts = maxNumberOfAttempts.Value };
            }
            
            if (retryInterval is not null)
            {
                c = c with { RetryInterval = retryInterval.Value };
            }
            
            return c;
        });
    }
}
