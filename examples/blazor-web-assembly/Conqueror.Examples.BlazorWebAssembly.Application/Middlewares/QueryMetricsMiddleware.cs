namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class QueryMetricsMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
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
        return pipeline.Use(new QueryMetricsMiddleware<TQuery, TResponse>());
    }
}
