namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

public static class DefaultPipelines
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline) =>
        // remove the common project prefix from the logger name to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerNameFactory = command => command.GetType().FullName?.Replace("Conqueror.Recipes.CQS.Advanced.CleanArchitecture.", ""))
                .UseDataAnnotationValidation();

    public static IQueryPipelineBuilder UseDefault(this IQueryPipelineBuilder pipeline) =>
        // remove the common project prefix from the logger name to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerNameFactory = query => query.GetType().FullName?.Replace("Conqueror.Recipes.CQS.Advanced.CleanArchitecture.", ""))
                .UseDataAnnotationValidation();
}
