using System.ComponentModel.DataAnnotations;

namespace Examples.BlazorWebAssembly.API.Middlewares;

public sealed class MessageValidationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        Validator.ValidateObject(ctx.Message, new(ctx.Message), true);

        return await ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}

public static class ValidationMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseValidation<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(new MessageValidationMiddleware<TMessage, TResponse>());
    }
}
