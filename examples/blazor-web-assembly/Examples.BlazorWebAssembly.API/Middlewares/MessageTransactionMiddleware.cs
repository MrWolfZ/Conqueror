namespace Examples.BlazorWebAssembly.API.Middlewares;

public sealed record MessageTransactionMiddlewareConfiguration
{
    public required bool EnlistInAmbientTransaction { get; set; }
}

public sealed class MessageTransactionMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required MessageTransactionMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}

public static class TransactionMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseTransaction<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        bool enlistInAmbientTransaction = true)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(new MessageTransactionMiddleware<TMessage, TResponse> { Configuration = new() { EnlistInAmbientTransaction = enlistInAmbientTransaction } });
    }

    public static IMessagePipeline<TMessage, TResponse> OutsideOfAmbientTransaction<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Configure<MessageTransactionMiddleware<TMessage, TResponse>>(m => m.Configuration.EnlistInAmbientTransaction = false);
    }
}
