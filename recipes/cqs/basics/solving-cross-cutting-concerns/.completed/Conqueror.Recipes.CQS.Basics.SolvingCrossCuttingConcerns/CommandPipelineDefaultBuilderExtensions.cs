namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class CommandPipelineDefaultBuilderExtensions
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAnnotationValidationCommandMiddleware>()
                       .UseRetry();
    }
}
