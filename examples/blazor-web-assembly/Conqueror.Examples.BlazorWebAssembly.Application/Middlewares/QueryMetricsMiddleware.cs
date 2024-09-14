namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class QueryMetricsMiddleware : IQueryMiddleware
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class MetricsQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseMetrics<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use(new QueryMetricsMiddleware());
    }
}
