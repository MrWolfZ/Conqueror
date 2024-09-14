namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record QueryAuthorizationMiddlewareConfiguration
{
    public string? Permission { get; set; }
}

public sealed class QueryAuthorizationMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public QueryAuthorizationMiddlewareConfiguration Configuration { get; } = new();

    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class AuthorizationQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use(new QueryAuthorizationMiddleware<TQuery, TResponse>());
    }

    public static IQueryPipeline<TQuery, TResponse> RequirePermission<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline, string permission)
        where TQuery : class
    {
        return pipeline.Configure<QueryAuthorizationMiddleware<TQuery, TResponse>>(m => m.Configuration.Permission = permission);
    }
}
