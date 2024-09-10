using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public static class DefaultCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseDefault<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.UseMetrics()
                       .UseLogging()
                       .UseAuthorization()
                       .UseValidation()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry()
                       .UseTransaction();
    }
}
