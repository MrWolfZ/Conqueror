namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record CommandRetryMiddlewareConfiguration
{
    public required int MaxNumberOfAttempts { get; set; }

    public required  TimeSpan RetryInterval { get; set; }
}

public sealed class CommandRetryMiddleware : ICommandMiddleware
{
    public required CommandRetryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class RetryCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                      int maxNumberOfAttempts = 3,
                                                                                      TimeSpan? retryInterval = null)
        where TCommand : class
    {
        var configuration = new CommandRetryMiddlewareConfiguration
        {
            MaxNumberOfAttempts = maxNumberOfAttempts,
            RetryInterval = retryInterval ?? TimeSpan.FromSeconds(1),
        };

        return pipeline.Use(new CommandRetryMiddleware { Configuration = configuration });
    }

    public static ICommandPipeline<TCommand, TResponse> ConfigureRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                            int? maxNumberOfAttempts = null,
                                                                                            TimeSpan? retryInterval = null)
        where TCommand : class
    {
        return pipeline.Configure<CommandRetryMiddleware>(m =>
        {
            if (maxNumberOfAttempts is not null)
            {
                m.Configuration.MaxNumberOfAttempts = maxNumberOfAttempts.Value;
            }

            if (retryInterval is not null)
            {
                m.Configuration.RetryInterval = retryInterval.Value;
            }
        });
    }
}
