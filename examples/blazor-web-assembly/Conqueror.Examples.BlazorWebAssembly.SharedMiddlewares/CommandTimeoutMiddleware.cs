namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record CommandTimeoutMiddlewareConfiguration(TimeSpan TimeoutAfter);

public sealed class CommandTimeoutMiddleware : ICommandMiddleware<CommandTimeoutMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandTimeoutMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class TimeoutCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseTimeout(this ICommandPipelineBuilder pipeline, TimeSpan timeoutAfter)
    {
        return pipeline.Use<CommandTimeoutMiddleware, CommandTimeoutMiddlewareConfiguration>(new(timeoutAfter));
    }

    public static ICommandPipelineBuilder ConfigureTimeout(this ICommandPipelineBuilder pipeline, TimeSpan timeoutAfter)
    {
        return pipeline.Configure<CommandTimeoutMiddleware, CommandTimeoutMiddlewareConfiguration>(new CommandTimeoutMiddlewareConfiguration(timeoutAfter));
    }
}
