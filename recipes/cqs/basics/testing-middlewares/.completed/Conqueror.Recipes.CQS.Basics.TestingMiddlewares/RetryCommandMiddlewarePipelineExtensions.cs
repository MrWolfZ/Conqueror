namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares;

internal static class RetryCommandMiddlewarePipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                      Action<RetryMiddlewareConfiguration>? configure = null)
        where TCommand : class
    {
        var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
        var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = defaultRetryAttemptLimit };
        configure?.Invoke(configuration);
        return pipeline.Use(new RetryCommandMiddleware { Configuration = configuration });
    }

    public static ICommandPipeline<TCommand, TResponse> ConfigureRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                            Action<RetryMiddlewareConfiguration> configure)
        where TCommand : class
    {
        return pipeline.Configure<RetryCommandMiddleware>(m => configure(m.Configuration));
    }

    public static ICommandPipeline<TCommand, TResponse> WithoutRetry<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Without<RetryCommandMiddleware>();
    }
}
