namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture;

public static class DefaultPipelines
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline) =>
        pipeline.UseLogging()
                .UseDataAnnotationValidation();

    public static IQueryPipelineBuilder UseDefault(this IQueryPipelineBuilder pipeline) =>
        pipeline.UseLogging()
                .UseDataAnnotationValidation();
}
