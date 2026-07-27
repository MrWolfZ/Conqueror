namespace Conqueror.Recipes.Messaging.CallingHttp.Middlewares;

public static class DataAnnotationValidationMiddlewarePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDataAnnotationValidation<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(new DataAnnotationValidationMiddleware<TMessage, TResponse>());
    }
}
