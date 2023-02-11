namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares
{
    internal static class DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions
    {
        public static ICommandPipelineBuilder UseDataAnnotationValidation(this ICommandPipelineBuilder pipeline)
        {
            return pipeline.Use<DataAnnotationValidationCommandMiddleware>();
        }
    }
}
