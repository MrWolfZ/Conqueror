using Conqueror.CQS.Middleware.Authentication;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ICommandPipelineBuilder" /> to add, configure, or remove authentication functionality.
/// </summary>
public static class ConquerorCqsMiddlewareAuthenticationCommandPipelineBuilderExtensions
{
    /// <summary>
    ///     Enable authentication functionality for a pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the authentication middleware to</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder UseAuthentication(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<AuthenticationCommandMiddleware, AuthenticationCommandMiddlewareConfiguration>(new());
    }

    /// <summary>
    ///     Enforce that an authenticated principal is present when executing the pipeline. If no such principal
    ///     is present, then a <see cref="ConquerorAuthenticationMissingPrincipalException" /> will be thrown during
    ///     pipeline execution. When a principal is present but not authenticated, then a
    ///     <see cref="ConquerorAuthenticationUnauthenticatedPrincipalException" /> will be thrown during pipeline
    ///     execution.
    /// </summary>
    /// <param name="pipeline">The command pipeline to configure authentication for</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder RequireAuthenticatedPrincipal(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Configure<AuthenticationCommandMiddleware, AuthenticationCommandMiddlewareConfiguration>(o => o.RequireAuthenticatedPrincipal = true);
    }

    /// <summary>
    ///     Allow the pipeline to execute without an authenticated principal being present.
    /// </summary>
    /// <param name="pipeline">The command pipeline to configure authentication for</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder AllowAnonymousAccess(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Configure<AuthenticationCommandMiddleware, AuthenticationCommandMiddlewareConfiguration>(o => o.RequireAuthenticatedPrincipal = false);
    }

    /// <summary>
    ///     Remove the authentication middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the authentication middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder WithoutAuthentication(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Without<AuthenticationCommandMiddleware, AuthenticationCommandMiddlewareConfiguration>();
    }
}
