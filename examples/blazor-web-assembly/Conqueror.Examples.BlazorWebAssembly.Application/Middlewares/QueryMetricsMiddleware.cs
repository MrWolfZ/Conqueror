namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class GatherQueryMetricsAttribute : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<QueryMetricsMiddleware>
{
}

public sealed class QueryMetricsMiddleware : IQueryMiddleware<GatherQueryMetricsAttribute>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, GatherQueryMetricsAttribute> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
