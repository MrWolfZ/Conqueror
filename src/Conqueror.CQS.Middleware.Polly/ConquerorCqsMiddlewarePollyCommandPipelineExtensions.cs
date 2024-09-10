using Conqueror.CQS.Middleware.Polly;
using Polly;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ICommandPipeline" /> to add, configure, or remove Polly functionality.
/// </summary>
public static class ConquerorCqsMiddlewarePollyCommandPipelineExtensions
{
    /// <summary>
    ///     Wrap the execution of the rest of the command pipeline in a Polly <see cref="Polly.AsyncPolicy" />.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the Polly middleware to</param>
    /// <param name="policy">The policy to use to wrap the rest of the pipeline execution</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> UsePolly<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                      AsyncPolicy policy)
        where TCommand : class
    {
        return pipeline.Use<PollyCommandMiddleware, PollyCommandMiddlewareConfiguration>(new() { Policy = policy });
    }

    /// <summary>
    ///     Set the <see cref="Polly.AsyncPolicy" /> to use in the Polly middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to configure the Polly middleware in</param>
    /// <param name="policy">The policy to use in the Polly middleware</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> ConfigurePollyPolicy<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                                  AsyncPolicy policy)
        where TCommand : class
    {
        return pipeline.Configure<PollyCommandMiddleware, PollyCommandMiddlewareConfiguration>(o => o.Policy = policy);
    }

    /// <summary>
    ///     Remove the Polly middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the Polly middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> WithoutPolly<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Without<PollyCommandMiddleware, PollyCommandMiddlewareConfiguration>();
    }
}
