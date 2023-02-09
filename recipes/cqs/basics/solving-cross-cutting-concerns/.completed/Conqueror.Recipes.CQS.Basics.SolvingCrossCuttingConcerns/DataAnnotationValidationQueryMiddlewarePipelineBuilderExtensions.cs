namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class DataAnnotationValidationQueryMiddlewarePipelineBuilderExtensions
{
    public static IQueryPipelineBuilder UseDataAnnotationValidation(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAnnotationValidationQueryMiddleware>();
    }
}
