namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class DataAnnotationValidationQueryMiddlewarePipelineBuilderExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseDataAnnotationValidation<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use<DataAnnotationValidationQueryMiddleware>();
    }
}
