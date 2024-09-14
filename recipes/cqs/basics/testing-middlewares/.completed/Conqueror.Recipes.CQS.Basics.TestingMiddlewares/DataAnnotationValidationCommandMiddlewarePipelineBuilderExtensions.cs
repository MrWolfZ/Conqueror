namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares;

internal static class DataAnnotationValidationCommandMiddlewarePipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseDataAnnotationValidation<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use(new DataAnnotationValidationCommandMiddleware());
    }
}
