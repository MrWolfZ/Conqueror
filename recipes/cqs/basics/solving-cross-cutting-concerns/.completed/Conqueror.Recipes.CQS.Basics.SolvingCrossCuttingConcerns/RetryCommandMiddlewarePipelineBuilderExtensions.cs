namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class RetryCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                      int? retryAttemptLimit = null)
        where TCommand : class
    {
        var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
        var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
        return pipeline.Use<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configuration);
    }

    public static ICommandPipeline<TCommand, TResponse> ConfigureRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                            Action<RetryMiddlewareConfiguration> configure)
        where TCommand : class
    {
        return pipeline.Configure<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configure);
    }

    public static ICommandPipeline<TCommand, TResponse> WithoutRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Without<RetryCommandMiddleware, RetryMiddlewareConfiguration>();
    }
}
