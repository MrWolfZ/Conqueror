namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class DataAnnotationValidationQueryMiddlewarePipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseDataAnnotationValidation<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use<DataAnnotationValidationQueryMiddleware>();
    }
}
