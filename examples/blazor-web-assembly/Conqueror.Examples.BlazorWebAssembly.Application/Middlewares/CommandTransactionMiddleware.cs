namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class ExecuteInTransactionAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<CommandTransactionMiddleware>
{
    public bool EnlistInAmbientTransaction { get; init; } = true;
}

public sealed class CommandTransactionMiddleware : ICommandMiddleware<ExecuteInTransactionAttribute>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, ExecuteInTransactionAttribute> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
