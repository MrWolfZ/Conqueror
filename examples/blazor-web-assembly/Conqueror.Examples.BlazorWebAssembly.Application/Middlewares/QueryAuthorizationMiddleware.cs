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

public static class AuthorizationQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use<QueryAuthorizationMiddleware, QueryAuthorizationMiddlewareConfiguration>(new(null));
    }

    public static IQueryPipeline<TQuery, TResponse> UsePermission<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline, string permission)
        where TQuery : class
    {
        return pipeline.Use<QueryAuthorizationMiddleware, QueryAuthorizationMiddlewareConfiguration>(new(permission));
    }

    public static IQueryPipeline<TQuery, TResponse> RequirePermission<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline, string permission)
        where TQuery : class
    {
        return pipeline.Configure<QueryAuthorizationMiddleware, QueryAuthorizationMiddlewareConfiguration>(c => c with { Permission = permission });
    }
}
