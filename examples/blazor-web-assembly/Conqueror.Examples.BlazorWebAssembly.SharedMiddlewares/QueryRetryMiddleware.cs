namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record QueryRetryMiddlewareConfiguration(int MaxNumberOfAttempts, TimeSpan RetryInterval);

public sealed class QueryRetryMiddleware : IQueryMiddleware<QueryRetryMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryRetryMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class RetryQueryPipelineBuilderExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                int maxNumberOfAttempts = 3,
                                                                                TimeSpan? retryInterval = null)
        where TQuery : class
    {
        var configuration = new QueryRetryMiddlewareConfiguration(maxNumberOfAttempts, retryInterval ?? TimeSpan.FromSeconds(1));

        return pipeline.Use<QueryRetryMiddleware, QueryRetryMiddlewareConfiguration>(configuration);
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                      int? maxNumberOfAttempts = null,
                                                                                      TimeSpan? retryInterval = null)
        where TQuery : class
    {
        return pipeline.Configure<QueryRetryMiddleware, QueryRetryMiddlewareConfiguration>(c =>
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
