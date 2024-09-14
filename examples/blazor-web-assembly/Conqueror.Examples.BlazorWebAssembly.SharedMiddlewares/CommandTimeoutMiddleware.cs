namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record CommandTimeoutMiddlewareConfiguration
{
    public required TimeSpan TimeoutAfter { get; set; }
}

public sealed class CommandTimeoutMiddleware : ICommandMiddleware
{
    public required CommandTimeoutMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class TimeoutCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseTimeout<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, TimeSpan timeoutAfter)
        where TCommand : class
    {
        return pipeline.Use(new CommandTimeoutMiddleware { Configuration = new() { TimeoutAfter = timeoutAfter } });
    }

    public static ICommandPipeline<TCommand, TResponse> ConfigureTimeout<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, TimeSpan timeoutAfter)
        where TCommand : class
    {
        return pipeline.Configure<CommandTimeoutMiddleware>(m => m.Configuration.TimeoutAfter = timeoutAfter);
    }
}
