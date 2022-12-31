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

public static class TransactionCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseTransaction(this ICommandPipelineBuilder pipeline, bool enlistInAmbientTransaction = true)
    {
        return pipeline.Use<CommandTransactionMiddleware, CommandTransactionMiddlewareConfiguration>(new(enlistInAmbientTransaction));
    }

    public static ICommandPipelineBuilder OutsideOfAmbientTransaction(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Configure<CommandTransactionMiddleware, CommandTransactionMiddlewareConfiguration>(c => c with { EnlistInAmbientTransaction = false });
    }
}
