namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class CacheQueryResultAttribute : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<QueryCachingMiddleware>
{
    public CacheQueryResultAttribute(int invalidateResultsAfterSeconds)
    {
        InvalidateResultsAfterSeconds = invalidateResultsAfterSeconds;
    }

    public int MaxCacheSizeInMegabytes { get; init; } = 10;
    
    public int InvalidateResultsAfterSeconds { get; init; }
    
    public TimeSpan InvalidateResultsAfter => TimeSpan.FromSeconds(InvalidateResultsAfterSeconds);

    public Type[] InvalidateResultsOnEventTypes { get; init; } = Array.Empty<Type>();
}

public sealed class QueryCachingMiddleware : IQueryMiddleware<CacheQueryResultAttribute>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, CacheQueryResultAttribute> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
