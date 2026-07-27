namespace Conqueror.Recipes.Messaging.CleanArchitecture;

public static class DefaultPipelines
{
    public static IMessagePipeline<TMessage, TResponse> UseDefault<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse> =>
        // remove the common project prefix from the logger category to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerCategoryFactory = message => message.GetType().FullName?.Replace("Conqueror.Recipes.Messaging.CleanArchitecture.", "", StringComparison.Ordinal) ?? "");
}
