namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record CommandTransactionMiddlewareConfiguration(bool EnlistInAmbientTransaction);

public sealed class CommandTransactionMiddleware : ICommandMiddleware<CommandTransactionMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandTransactionMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class TransactionCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseTransaction<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline, bool enlistInAmbientTransaction = true)
        where TCommand : class
    {
        return pipeline.Use<CommandTransactionMiddleware, CommandTransactionMiddlewareConfiguration>(new(enlistInAmbientTransaction));
    }

    public static ICommandPipeline<TCommand, TResponse> OutsideOfAmbientTransaction<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Configure<CommandTransactionMiddleware, CommandTransactionMiddlewareConfiguration>(c => c with { EnlistInAmbientTransaction = false });
    }
}
