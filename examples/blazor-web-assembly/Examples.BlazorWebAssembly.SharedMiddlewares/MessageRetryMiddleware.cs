using Conqueror;

namespace Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record MessageRetryMiddlewareConfiguration
{
    public required int MaxNumberOfAttempts { get; set; }

    public required TimeSpan RetryInterval { get; set; }
}

public sealed class MessageRetryMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required MessageRetryMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}

public static class RetryMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseRetry<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        int maxNumberOfAttempts = 3,
        TimeSpan? retryInterval = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var configuration = new MessageRetryMiddlewareConfiguration
        {
            MaxNumberOfAttempts = maxNumberOfAttempts,
            RetryInterval = retryInterval ?? TimeSpan.FromSeconds(1),
        };

        return pipeline.Use(new MessageRetryMiddleware<TMessage, TResponse> { Configuration = configuration });
    }

    public static IMessagePipeline<TMessage, TResponse> ConfigureRetry<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        int? maxNumberOfAttempts = null,
        TimeSpan? retryInterval = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Configure<MessageRetryMiddleware<TMessage, TResponse>>(m =>
        {
            if (maxNumberOfAttempts is not null)
            {
                m.Configuration.MaxNumberOfAttempts = maxNumberOfAttempts.Value;
            }

            if (retryInterval is not null)
            {
                m.Configuration.RetryInterval = retryInterval.Value;
            }
        });
    }
}
