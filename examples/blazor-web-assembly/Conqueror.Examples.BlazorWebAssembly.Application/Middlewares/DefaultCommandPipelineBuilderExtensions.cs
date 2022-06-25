namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public static class DefaultCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.UseMetrics()
                       .UseLogging()
                       .UseAuthorization()
                       .UseValidation()
                       .UseRetry()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseTransaction();
    }
}
