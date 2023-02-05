namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class QueryPipelineDefaultBuilderExtensions
{
    public static IQueryPipelineBuilder UseDefault(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAnnotationValidationQueryMiddleware>()
                       .UseRetry();
    }
}
