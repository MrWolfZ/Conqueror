namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares;

internal static class CommandPipelineDefaultBuilderExtensions
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.UseDataAnnotationValidation()
                       .UseRetry();
    }
}
