namespace Examples.BlazorWebAssembly.API.Middlewares;

public sealed record MessageAuthorizationMiddlewareConfiguration
{
    public string? Permission { get; set; }
}

public sealed class MessageAuthorizationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public MessageAuthorizationMiddlewareConfiguration Configuration { get; } = new();

    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}

public static class AuthorizationMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseAuthorization<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(new MessageAuthorizationMiddleware<TMessage, TResponse>());
    }

    public static IMessagePipeline<TMessage, TResponse> RequirePermission<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline, string permission)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Configure<MessageAuthorizationMiddleware<TMessage, TResponse>>(m => m.Configuration.Permission = permission);
    }
}
