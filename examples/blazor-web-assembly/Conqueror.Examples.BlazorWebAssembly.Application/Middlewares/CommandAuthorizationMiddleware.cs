namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record CommandAuthorizationMiddlewareConfiguration
{
    public string? Permission { get; set; }
}

public sealed class CommandAuthorizationMiddleware : ICommandMiddleware
{
    public CommandAuthorizationMiddlewareConfiguration Configuration { get; } = new();

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class AuthorizationCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use(new CommandAuthorizationMiddleware());
    }

    public static ICommandPipeline<TCommand, TResponse> RequirePermission<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, string permission)
        where TCommand : class
    {
        return pipeline.Configure<CommandAuthorizationMiddleware>(m => m.Configuration.Permission = permission);
    }
}
