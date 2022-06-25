namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;


public sealed record CommandAuthorizationMiddlewareConfiguration(string? Permission);

public sealed class CommandAuthorizationMiddleware : ICommandMiddleware<CommandAuthorizationMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandAuthorizationMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class AuthorizationCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseAuthorization(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<CommandAuthorizationMiddleware, CommandAuthorizationMiddlewareConfiguration>(new(null));
    }

    public static ICommandPipelineBuilder UsePermission(this ICommandPipelineBuilder pipeline, string permission)
    {
        return pipeline.Use<CommandAuthorizationMiddleware, CommandAuthorizationMiddlewareConfiguration>(new(permission));
    }

    public static ICommandPipelineBuilder RequirePermission(this ICommandPipelineBuilder pipeline, string permission)
    {
        return pipeline.Configure<CommandAuthorizationMiddleware, CommandAuthorizationMiddlewareConfiguration>(c => c with { Permission = permission });
    }
}
