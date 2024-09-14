namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record QueryAuthorizationMiddlewareConfiguration
{
    public string? Permission { get; set; }
}

public sealed class QueryAuthorizationMiddleware : IQueryMiddleware
{
    public QueryAuthorizationMiddlewareConfiguration Configuration { get; } = new();

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
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
        return pipeline.Use(new QueryAuthorizationMiddleware());
    }

    public static IQueryPipeline<TQuery, TResponse> RequirePermission<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline, string permission)
        where TQuery : class
    {
        return pipeline.Configure<QueryAuthorizationMiddleware>(m => m.Configuration.Permission = permission);
    }
}
