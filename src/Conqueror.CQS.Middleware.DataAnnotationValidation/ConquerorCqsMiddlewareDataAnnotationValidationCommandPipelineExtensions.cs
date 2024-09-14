using Conqueror.CQS.Middleware.DataAnnotationValidation;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ICommandPipeline{TCommand,TResponse}" /> to add, configure, or remove data annotation validation functionality.
/// </summary>
public static class ConquerorCqsMiddlewareDataAnnotationValidationCommandPipelineExtensions
{
    /// <summary>
    ///     Add data annotation validation functionality to a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add data annotation validation to</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> UseDataAnnotationValidation<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use(new DataAnnotationValidationCommandMiddleware());
    }

    /// <summary>
    ///     Remove the data annotation validation middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the data annotation validation middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> WithoutDataAnnotationValidation<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Without<DataAnnotationValidationCommandMiddleware>();
    }
}
