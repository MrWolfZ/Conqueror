namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record QueryRetryMiddlewareConfiguration
{
    public required int MaxNumberOfAttempts { get; set; }

    public required  TimeSpan RetryInterval { get; set; }
}

public sealed class QueryRetryMiddleware : IQueryMiddleware
{
    public required QueryRetryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class RetryQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                int maxNumberOfAttempts = 3,
                                                                                TimeSpan? retryInterval = null)
        where TQuery : class
    {
        var configuration = new QueryRetryMiddlewareConfiguration
        {
            MaxNumberOfAttempts = maxNumberOfAttempts,
            RetryInterval = retryInterval ?? TimeSpan.FromSeconds(1),
        };

        return pipeline.Use(new QueryRetryMiddleware { Configuration = configuration });
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureRetry<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                      int? maxNumberOfAttempts = null,
                                                                                      TimeSpan? retryInterval = null)
        where TQuery : class
    {
        return pipeline.Configure<QueryRetryMiddleware>(m =>
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
