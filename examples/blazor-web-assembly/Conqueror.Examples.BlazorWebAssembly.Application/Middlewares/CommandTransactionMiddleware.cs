namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record CommandTransactionMiddlewareConfiguration
{
    public required bool EnlistInAmbientTransaction { get; set; }
}

public sealed class CommandTransactionMiddleware : ICommandMiddleware
{
    public required CommandTransactionMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class TransactionCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseTransaction<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                            bool enlistInAmbientTransaction = true)
        where TCommand : class
    {
        return pipeline.Use(new CommandTransactionMiddleware { Configuration = new() { EnlistInAmbientTransaction = enlistInAmbientTransaction } });
    }

    public static ICommandPipeline<TCommand, TResponse> OutsideOfAmbientTransaction<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Configure<CommandTransactionMiddleware>(m => m.Configuration.EnlistInAmbientTransaction = false);
    }
}
