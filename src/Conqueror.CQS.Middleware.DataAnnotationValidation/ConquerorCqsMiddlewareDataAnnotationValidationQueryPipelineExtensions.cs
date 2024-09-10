using Conqueror.CQS.Middleware.DataAnnotationValidation;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IQueryPipeline" /> to add, configure, or remove data annotation validation functionality.
/// </summary>
public static class ConquerorCqsMiddlewareDataAnnotationValidationQueryPipelineExtensions
{
    /// <summary>
    ///     Add data annotation validation functionality to a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add data annotation validation to</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> UseDataAnnotationValidation<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use<DataAnnotationValidationQueryMiddleware>();
    }

    /// <summary>
    ///     Remove the data annotation validation middleware from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the data annotation validation middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> WithoutDataAnnotationValidation<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Without<DataAnnotationValidationQueryMiddleware>();
    }
}
