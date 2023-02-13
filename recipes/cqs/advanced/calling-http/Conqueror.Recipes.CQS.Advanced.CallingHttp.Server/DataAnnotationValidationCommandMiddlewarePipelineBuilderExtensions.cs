namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

public static class DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseDataAnnotationValidation(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAnnotationValidationCommandMiddleware>();
    }
}
