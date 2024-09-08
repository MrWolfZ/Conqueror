namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class QueryPipelineDefaultBuilderExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseDefault<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.UseDataAnnotationValidation()
                       .UseRetry();
    }
}
