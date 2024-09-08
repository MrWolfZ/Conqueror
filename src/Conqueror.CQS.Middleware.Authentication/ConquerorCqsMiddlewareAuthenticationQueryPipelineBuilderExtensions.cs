using Conqueror.CQS.Middleware.Authentication;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IQueryPipeline" /> to add, configure, or remove authentication functionality.
/// </summary>
public static class ConquerorCqsMiddlewareAuthenticationQueryPipelineBuilderExtensions
{
    /// <summary>
    ///     Enable authentication functionality for a pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the authentication middleware to</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> UseAuthentication<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use<AuthenticationQueryMiddleware, AuthenticationQueryMiddlewareConfiguration>(new());
    }

    /// <summary>
    ///     Enforce that an authenticated principal is present when executing the pipeline. If no such principal
    ///     is present, then a <see cref="ConquerorAuthenticationMissingPrincipalException" /> will be thrown during
    ///     pipeline execution. When a principal is present but not authenticated, then a
    ///     <see cref="ConquerorAuthenticationUnauthenticatedPrincipalException" /> will be thrown during pipeline
    ///     execution.
    /// </summary>
    /// <param name="pipeline">The query pipeline to configure authentication for</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> RequireAuthenticatedPrincipal<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Configure<AuthenticationQueryMiddleware, AuthenticationQueryMiddlewareConfiguration>(o => o.RequireAuthenticatedPrincipal = true);
    }

    /// <summary>
    ///     Allow the pipeline to execute without an authenticated principal being present.
    /// </summary>
    /// <param name="pipeline">The query pipeline to configure authentication for</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> AllowAnonymousAccess<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Configure<AuthenticationQueryMiddleware, AuthenticationQueryMiddlewareConfiguration>(o => o.RequireAuthenticatedPrincipal = false);
    }

    /// <summary>
    ///     Remove the authentication middleware from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the authentication middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> WithoutAuthentication<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Without<AuthenticationQueryMiddleware, AuthenticationQueryMiddlewareConfiguration>();
    }
}
