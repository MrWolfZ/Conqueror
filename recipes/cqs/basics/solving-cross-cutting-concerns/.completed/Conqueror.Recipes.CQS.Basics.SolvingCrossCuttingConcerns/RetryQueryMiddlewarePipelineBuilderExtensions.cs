namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class RetryQueryMiddlewarePipelineBuilderExtensions
{
    public static IQueryPipelineBuilder UseRetry(this IQueryPipelineBuilder pipeline, int? retryAttemptLimit = null)
    {
        var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
        var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
        return pipeline.Use<RetryQueryMiddleware, RetryMiddlewareConfiguration>(configuration);
    }

    public static IQueryPipelineBuilder ConfigureRetry(this IQueryPipelineBuilder pipeline, Action<RetryMiddlewareConfiguration> configure)
    {
        return pipeline.Configure<RetryQueryMiddleware, RetryMiddlewareConfiguration>(configure);
    }

    public static IQueryPipelineBuilder WithoutRetry(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Without<RetryQueryMiddleware, RetryMiddlewareConfiguration>();
    }
}
