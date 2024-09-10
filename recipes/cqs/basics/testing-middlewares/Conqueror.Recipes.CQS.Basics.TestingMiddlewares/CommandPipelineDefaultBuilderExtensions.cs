namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares;

internal static class CommandPipelineDefaultBuilderExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseDefault<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.UseDataAnnotationValidation()
                       .UseRetry();
    }
}
