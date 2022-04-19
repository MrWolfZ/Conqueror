namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class RequiresCommandPermissionAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<CommandAuthorizationMiddleware>
{
    public RequiresCommandPermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

public sealed class CommandAuthorizationMiddleware : ICommandMiddleware<RequiresCommandPermissionAttribute>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, RequiresCommandPermissionAttribute> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
