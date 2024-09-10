namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Core.Application;

public static class DefaultPipelines
{
    public static ICommandPipeline<TCommand, TResponse> UseDefault<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class =>
        // remove the common project prefix from the logger name to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerNameFactory = command => command.GetType().FullName?.Replace("Conqueror.Recipes.CQS.Advanced.CleanArchitecture.", ""))
                .UseDataAnnotationValidation();

    public static IQueryPipeline<TQuery, TResponse> UseDefault<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class =>
        // remove the common project prefix from the logger name to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerNameFactory = query => query.GetType().FullName?.Replace("Conqueror.Recipes.CQS.Advanced.CleanArchitecture.", ""))
                .UseDataAnnotationValidation();
}
