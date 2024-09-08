namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record QueryCachingMiddlewareConfiguration(TimeSpan InvalidateResultsAfter)
{
    public int MaxCacheSizeInMegabytes { get; init; } = 10;

    public Type[] InvalidateResultsOnEventTypes { get; init; } = Array.Empty<Type>();
}

public sealed class QueryCachingMiddleware : IQueryMiddleware<QueryCachingMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryCachingMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class CachingQueryPipelineBuilderExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseCaching<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                  TimeSpan invalidateResultsAfter,
                                                                                  int? maxCacheSizeInMegabytes = null,
                                                                                  Type[]? invalidateResultsOnEventTypes = null)
        where TQuery : class
    {
        var configuration = new QueryCachingMiddlewareConfiguration(invalidateResultsAfter);
        return pipeline.Use<QueryCachingMiddleware, QueryCachingMiddlewareConfiguration>(configuration)
                       .ConfigureCaching(invalidateResultsAfter, maxCacheSizeInMegabytes, invalidateResultsOnEventTypes);
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureCaching<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                        TimeSpan invalidateResultsAfter,
                                                                                        int? maxCacheSizeInMegabytes = null,
                                                                                        Type[]? invalidateResultsOnEventTypes = null)
        where TQuery : class
    {
        return pipeline.Configure<QueryCachingMiddleware, QueryCachingMiddlewareConfiguration>(c =>
        {
            c = c with { InvalidateResultsAfter = invalidateResultsAfter };

            if (maxCacheSizeInMegabytes is not null)
            {
                c = c with { MaxCacheSizeInMegabytes = maxCacheSizeInMegabytes.Value };
            }

            if (invalidateResultsOnEventTypes is not null)
            {
                c = c with { InvalidateResultsOnEventTypes = invalidateResultsOnEventTypes };
            }

            return c;
        });
    }
}
