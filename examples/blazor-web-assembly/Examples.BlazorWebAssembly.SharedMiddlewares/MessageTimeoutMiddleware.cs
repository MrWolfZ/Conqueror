using Conqueror;

namespace Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record MessageTimeoutMiddlewareConfiguration
{
    public required TimeSpan TimeoutAfter { get; set; }
}

public sealed class MessageTimeoutMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required MessageTimeoutMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}

public static class TimeoutMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseTimeout<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        TimeSpan timeoutAfter)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(
            new MessageTimeoutMiddleware<TMessage, TResponse>
            {
                Configuration = new() { TimeoutAfter = timeoutAfter },
            });
    }

    public static IMessagePipeline<TMessage, TResponse> ConfigureTimeout<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        TimeSpan timeoutAfter)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Configure<MessageTimeoutMiddleware<TMessage, TResponse>>(m => m.Configuration.TimeoutAfter = timeoutAfter);
    }
}
