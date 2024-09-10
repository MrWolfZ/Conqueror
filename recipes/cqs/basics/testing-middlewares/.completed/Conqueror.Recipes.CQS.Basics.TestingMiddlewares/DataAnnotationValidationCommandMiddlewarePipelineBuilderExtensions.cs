namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares;

internal static class DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseDataAnnotationValidation<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use<DataAnnotationValidationCommandMiddleware>();
    }
}
