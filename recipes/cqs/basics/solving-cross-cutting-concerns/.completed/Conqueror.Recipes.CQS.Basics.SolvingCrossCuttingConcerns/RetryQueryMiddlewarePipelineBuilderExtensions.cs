namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class RetryQueryMiddlewarePipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                int? retryAttemptLimit = null)
        where TQuery : class
    {
        var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
        var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
        return pipeline.Use<RetryQueryMiddleware, RetryMiddlewareConfiguration>(configuration);
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                      Action<RetryMiddlewareConfiguration> configure)
        where TQuery : class
    {
        return pipeline.Configure<RetryQueryMiddleware, RetryMiddlewareConfiguration>(configure);
    }

    public static IQueryPipeline<TQuery, TResponse> WithoutRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Without<RetryQueryMiddleware, RetryMiddlewareConfiguration>();
    }
}
