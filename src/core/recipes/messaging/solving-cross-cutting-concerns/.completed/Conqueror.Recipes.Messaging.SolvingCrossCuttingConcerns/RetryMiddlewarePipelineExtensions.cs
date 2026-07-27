namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal static class RetryMiddlewarePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline,
                                                                                      int? retryAttemptLimit = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
        var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
        return pipeline.Use(new RetryMiddleware<TMessage, TResponse> { Configuration = configuration });
    }

    public static IMessagePipeline<TMessage, TResponse> ConfigureRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline,
                                                                                            Action<RetryMiddlewareConfiguration> configure)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Configure<RetryMiddleware<TMessage, TResponse>>(m => configure(m.Configuration));
    }

    public static IMessagePipeline<TMessage, TResponse> WithoutRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Without<RetryMiddleware<TMessage, TResponse>>();
    }
}
