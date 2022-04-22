namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class RequiresQueryPermissionAttribute : QueryMiddlewareConfigurationAttribute
{
    public RequiresQueryPermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

public sealed class QueryAuthorizationMiddleware : IQueryMiddleware<RequiresQueryPermissionAttribute>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, RequiresQueryPermissionAttribute> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
