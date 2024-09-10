namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class CommandMetricsMiddleware : ICommandMiddleware
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class MetricsCommandPipelineBuilderExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseMetrics<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use<CommandMetricsMiddleware>();
    }
}
