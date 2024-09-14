namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record CommandAuthorizationMiddlewareConfiguration
{
    public string? Permission { get; set; }
}

public sealed class CommandAuthorizationMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public CommandAuthorizationMiddlewareConfiguration Configuration { get; } = new();

    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
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
        return pipeline.Use(new CommandAuthorizationMiddleware<TCommand, TResponse>());
    }

    public static ICommandPipeline<TCommand, TResponse> RequirePermission<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, string permission)
        where TCommand : class
    {
        return pipeline.Configure<CommandAuthorizationMiddleware<TCommand, TResponse>>(m => m.Configuration.Permission = permission);
    }
}
