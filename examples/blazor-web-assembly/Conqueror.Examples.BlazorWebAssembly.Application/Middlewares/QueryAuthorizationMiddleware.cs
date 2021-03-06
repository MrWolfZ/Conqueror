namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record QueryAuthorizationMiddlewareConfiguration(string? Permission);

public sealed class QueryAuthorizationMiddleware : IQueryMiddleware<QueryAuthorizationMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryAuthorizationMiddlewareConfiguration> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class AuthorizationQueryPipelineBuilderExtensions
{
    public static IQueryPipelineBuilder UseAuthorization(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Use<QueryAuthorizationMiddleware, QueryAuthorizationMiddlewareConfiguration>(new(null));
    }

    public static IQueryPipelineBuilder UsePermission(this IQueryPipelineBuilder pipeline, string permission)
    {
        return pipeline.Use<QueryAuthorizationMiddleware, QueryAuthorizationMiddlewareConfiguration>(new(permission));
    }

    public static IQueryPipelineBuilder RequirePermission(this IQueryPipelineBuilder pipeline, string permission)
    {
        return pipeline.Configure<QueryAuthorizationMiddleware, QueryAuthorizationMiddlewareConfiguration>(c => c with { Permission = permission });
    }
}
