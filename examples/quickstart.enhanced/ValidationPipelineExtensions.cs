using System.ComponentModel.DataAnnotations;
using Conqueror;

namespace Quickstart.Enhanced;

public static class ValidationPipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDataAnnotationValidation<
        TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(ctx =>
        {
            Validator.ValidateObject(ctx.Message, new(ctx.Message), true);
            return ctx.Next(ctx.Message, ctx.CancellationToken);
        });
    }
}
