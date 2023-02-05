namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class RetryCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseRetry(this ICommandPipelineBuilder pipeline, int? retryAttemptLimit = null)
    {
        var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
        var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
        return pipeline.Use<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configuration);
    }

    public static ICommandPipelineBuilder ConfigureRetry(this ICommandPipelineBuilder pipeline, Action<RetryMiddlewareConfiguration> configure)
    {
        return pipeline.Configure<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configure);
    }

    public static ICommandPipelineBuilder WithoutRetry(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Without<RetryCommandMiddleware, RetryMiddlewareConfiguration>();
    }
}
