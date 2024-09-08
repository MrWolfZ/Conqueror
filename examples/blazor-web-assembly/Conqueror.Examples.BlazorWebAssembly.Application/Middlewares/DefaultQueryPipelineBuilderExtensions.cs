using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public static class DefaultQueryPipelineBuilderExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseDefault<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.UseMetrics()
                       .UseLogging()
                       .UseAuthorization()
                       .UseValidation()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry();
    }
}
