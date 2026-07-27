namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal static class MessagePipelineDefaultExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDefault<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.UseDataAnnotationValidation()
                       .UseRetry();
    }
}
