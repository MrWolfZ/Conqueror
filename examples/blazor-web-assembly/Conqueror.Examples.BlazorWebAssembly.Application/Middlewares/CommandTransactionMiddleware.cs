namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record CommandTransactionMiddlewareConfiguration
{
    public required bool EnlistInAmbientTransaction { get; set; }
}

public sealed class CommandTransactionMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public required CommandTransactionMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
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
        return pipeline.Use(new CommandTransactionMiddleware<TCommand, TResponse> { Configuration = new() { EnlistInAmbientTransaction = enlistInAmbientTransaction } });
    }

    public static ICommandPipeline<TCommand, TResponse> OutsideOfAmbientTransaction<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Configure<CommandTransactionMiddleware<TCommand, TResponse>>(m => m.Configuration.EnlistInAmbientTransaction = false);
    }
}
