namespace Examples.BlazorWebAssembly.API.Middlewares;

public sealed class MessageMetricsMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}

public static class MetricsMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseMetrics<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(new MessageMetricsMiddleware<TMessage, TResponse>());
    }
}
