namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public static class DefaultQueryPipelineBuilderExtensions
{
    public static IQueryPipelineBuilder UseDefault(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.UseMetrics()
                       .UseLogging()
                       .UseAuthorization()
                       .UseValidation()
                       .UseTimeout(TimeSpan.FromMinutes(1));
    }
}
