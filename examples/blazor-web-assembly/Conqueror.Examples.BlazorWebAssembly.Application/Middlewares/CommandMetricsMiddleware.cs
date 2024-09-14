namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class CommandMetricsMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class MetricsCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseMetrics<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use(new CommandMetricsMiddleware<TCommand, TResponse>());
    }
}
