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
    public static IQueryPipelineBuilder UseRetry(this IQueryPipelineBuilder pipeline,
                                                 int maxNumberOfAttempts = 3,
                                                 TimeSpan? retryInterval = null)
    {
        var configuration = new QueryRetryMiddlewareConfiguration(maxNumberOfAttempts, retryInterval ?? TimeSpan.FromSeconds(1));

        return pipeline.Use<QueryRetryMiddleware, QueryRetryMiddlewareConfiguration>(configuration);
    }

    public static IQueryPipelineBuilder ConfigureRetry(this IQueryPipelineBuilder pipeline,
                                                       int? maxNumberOfAttempts = null,
                                                       TimeSpan? retryInterval = null)
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
