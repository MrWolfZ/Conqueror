using Conqueror.CQS.Middleware.Authorization;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IQueryPipelineBuilder" /> to add, configure, or remove functional authorization functionality.
/// </summary>
public static class ConquerorCqsMiddlewareAuthorizationQueryPipelineBuilderExtensions
{
    /// <summary>
    ///     Perform a functional authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.GetCurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the query, then a <see cref="ConquerorFunctionalAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow queries which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the functional authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking operation authorization</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder UseFunctionalAuthorization(this IQueryPipelineBuilder pipeline, ConquerorFunctionalAuthorizationCheck authorizationCheck)
    {
        return pipeline.Use<FunctionalAuthorizationQueryMiddleware, FunctionalAuthorizationQueryMiddlewareConfiguration>(new(authorizationCheck));
    }

    /// <summary>
    ///     Remove the functional authorization middleware from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the functional authorization middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder WithoutFunctionalAuthorization(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Without<FunctionalAuthorizationQueryMiddleware, FunctionalAuthorizationQueryMiddlewareConfiguration>();
    }
}
