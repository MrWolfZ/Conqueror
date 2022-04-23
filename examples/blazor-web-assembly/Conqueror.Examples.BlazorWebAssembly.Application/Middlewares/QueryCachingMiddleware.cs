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
        var configuration = new QueryCachingMiddlewareConfiguration(invalidateResultsAfter)
        {
            MaxCacheSizeInMegabytes = maxCacheSizeInMegabytes ?? 10,
            InvalidateResultsOnEventTypes = invalidateResultsOnEventTypes ?? Array.Empty<Type>(),
        };

        return pipeline.Use<QueryCachingMiddleware, QueryCachingMiddlewareConfiguration>(configuration);
    }
}
