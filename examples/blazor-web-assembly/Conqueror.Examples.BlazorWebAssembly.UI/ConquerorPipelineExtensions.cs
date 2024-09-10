using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.UI;

public static class ConquerorPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseDefaultClientPipeline<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.UseLogging()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry();
    }

    public static ICommandHandler<TCommand, TResponse> WithDefaultClientPipeline<TCommand, TResponse>(this ICommandHandler<TCommand, TResponse> handler)
        where TCommand : class
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefaultClientPipeline());
    }

    public static ICommandHandler<TCommand> WithDefaultClientPipeline<TCommand>(this ICommandHandler<TCommand> handler)
        where TCommand : class
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefaultClientPipeline());
    }

    public static IQueryPipeline<TQuery, TResponse> UseDefaultClientPipeline<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.UseLogging()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry();
    }

    public static IQueryHandler<TQuery, TResponse> WithDefaultClientPipeline<TQuery, TResponse>(this IQueryHandler<TQuery, TResponse> handler)
        where TQuery : class
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefaultClientPipeline());
    }
}
