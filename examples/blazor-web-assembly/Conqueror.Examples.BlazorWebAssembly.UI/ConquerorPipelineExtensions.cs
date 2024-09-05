using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.UI;

public static class ConquerorPipelineExtensions
{
    public static ICommandPipelineBuilder UseDefaultClientPipeline(this ICommandPipelineBuilder pipeline)
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

    public static IQueryPipelineBuilder UseDefaultClientPipeline(this IQueryPipelineBuilder pipeline)
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