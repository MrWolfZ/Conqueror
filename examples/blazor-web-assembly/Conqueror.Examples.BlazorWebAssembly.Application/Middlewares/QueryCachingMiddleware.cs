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
    public static IQueryPipelineBuilder UseCaching(this IQueryPipelineBuilder pipeline,
                                                   TimeSpan invalidateResultsAfter,
                                                   int? maxCacheSizeInMegabytes = null,
                                                   Type[]? invalidateResultsOnEventTypes = null)
    {
        var configuration = new QueryCachingMiddlewareConfiguration(invalidateResultsAfter);
        return pipeline.Use<QueryCachingMiddleware, QueryCachingMiddlewareConfiguration>(configuration)
                       .ConfigureCaching(invalidateResultsAfter, maxCacheSizeInMegabytes, invalidateResultsOnEventTypes);
    }

    public static IQueryPipelineBuilder ConfigureCaching(this IQueryPipelineBuilder pipeline,
                                                         TimeSpan invalidateResultsAfter,
                                                         int? maxCacheSizeInMegabytes = null,
                                                         Type[]? invalidateResultsOnEventTypes = null)
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
