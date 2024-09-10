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
    public static ICommandPipeline<TCommand, TResponse> UseAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use<CommandAuthorizationMiddleware, CommandAuthorizationMiddlewareConfiguration>(new(null));
    }

    public static ICommandPipeline<TCommand, TResponse> UsePermission<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, string permission)
        where TCommand : class
    {
        return pipeline.Use<CommandAuthorizationMiddleware, CommandAuthorizationMiddlewareConfiguration>(new(permission));
    }

    public static ICommandPipeline<TCommand, TResponse> RequirePermission<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, string permission)
        where TCommand : class
    {
        return pipeline.Configure<CommandAuthorizationMiddleware, CommandAuthorizationMiddlewareConfiguration>(c => c with { Permission = permission });
    }
}
