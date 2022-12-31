using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.UI;

public static class ConquerorPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseDefaultHttpPipeline(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.UseLogging()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry();
    }

    public static IQueryPipelineBuilder UseDefaultHttpPipeline(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.UseLogging()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry();
    }
}
