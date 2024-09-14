namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record QueryCachingMiddlewareConfiguration
{
    public required TimeSpan InvalidateResultsAfter { get; set; }

    public int MaxCacheSizeInMegabytes { get; set; } = 10;

    public Type[] InvalidateResultsOnEventTypes { get; set; } = Array.Empty<Type>();
}

public sealed class QueryCachingMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public required QueryCachingMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class CachingQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseCaching<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                  TimeSpan invalidateResultsAfter,
                                                                                  int? maxCacheSizeInMegabytes = null,
                                                                                  Type[]? invalidateResultsOnEventTypes = null)
        where TQuery : class
    {
        var configuration = new QueryCachingMiddlewareConfiguration { InvalidateResultsAfter = invalidateResultsAfter };
        return pipeline.Use(new QueryCachingMiddleware<TQuery, TResponse> { Configuration = configuration })
                       .ConfigureCaching(invalidateResultsAfter, maxCacheSizeInMegabytes, invalidateResultsOnEventTypes);
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureCaching<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                        TimeSpan invalidateResultsAfter,
                                                                                        int? maxCacheSizeInMegabytes = null,
                                                                                        Type[]? invalidateResultsOnEventTypes = null)
        where TQuery : class
    {
        return pipeline.Configure<QueryCachingMiddleware<TQuery, TResponse>>(m =>
        {
            m.Configuration.InvalidateResultsAfter = invalidateResultsAfter;

            if (maxCacheSizeInMegabytes is not null)
            {
                m.Configuration.MaxCacheSizeInMegabytes = maxCacheSizeInMegabytes.Value;
            }

            if (invalidateResultsOnEventTypes is not null)
            {
                m.Configuration.InvalidateResultsOnEventTypes = invalidateResultsOnEventTypes;
            }
        });
    }
}
