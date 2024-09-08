namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Core.Application;

public static class DefaultPipelines
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline) =>
        // remove the common project prefix from the logger name to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerNameFactory = command => command.GetType().FullName?.Replace("Conqueror.Recipes.CQS.Advanced.MonoToDistri.", ""))
                .UseDataAnnotationValidation();

    public static ICommandHandler<TCommand, TResponse> WithDefaultClientPipeline<TCommand, TResponse>(this ICommandHandler<TCommand, TResponse> handler)
        where TCommand : class
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefault());
    }

    public static ICommandHandler<TCommand> WithDefaultClientPipeline<TCommand>(this ICommandHandler<TCommand> handler)
        where TCommand : class
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefault());
    }

    public static IQueryPipeline<TQuery, TResponse> UseDefault<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class =>
        // remove the common project prefix from the logger name to reduce the noise in the logs
        pipeline.UseLogging(o => o.LoggerNameFactory = query => query.GetType().FullName?.Replace("Conqueror.Recipes.CQS.Advanced.MonoToDistri.", ""))
                .UseDataAnnotationValidation();

    public static IQueryHandler<TQuery, TResponse> WithDefaultClientPipeline<TQuery, TResponse>(this IQueryHandler<TQuery, TResponse> handler)
        where TQuery : class
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefault());
    }
}
