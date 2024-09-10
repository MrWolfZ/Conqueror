namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

public static class DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseDataAnnotationValidation<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use<DataAnnotationValidationCommandMiddleware>();
    }
}
