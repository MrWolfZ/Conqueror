namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class GatherCommandMetricsAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<CommandMetricsMiddleware>
{
}

public sealed class CommandMetricsMiddleware : ICommandMiddleware<GatherCommandMetricsAttribute>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, GatherCommandMetricsAttribute> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
