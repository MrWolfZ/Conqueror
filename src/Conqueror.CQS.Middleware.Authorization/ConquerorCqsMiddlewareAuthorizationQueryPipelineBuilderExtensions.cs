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

    /// <summary>
    ///     Enable data authorization functionality for a pipeline. By default, this middleware will not perform
    ///     any authorization checks. Use <see cref="AddDataAuthorizationCheck{TQuery}" /> to add checks.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the data authorization middleware to</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder UseDataAuthorization(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAuthorizationQueryMiddleware, DataAuthorizationQueryMiddlewareConfiguration>(new());
    }

    /// <summary>
    ///     Perform a data authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.GetCurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the query, then a <see cref="ConquerorDataAuthorizationFailedException" />
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
    public static IQueryPipelineBuilder AddDataAuthorizationCheck<TQuery>(this IQueryPipelineBuilder pipeline, ConquerorDataAuthorizationCheck<TQuery> authorizationCheck)
        where TQuery : class
    {
        return pipeline.Configure<DataAuthorizationQueryMiddleware, DataAuthorizationQueryMiddlewareConfiguration>(o =>
        {
            // we assume that the user passed in a compatible query type, so we perform a simple cast
            o.AuthorizationChecks.Add((principal, query) => authorizationCheck(principal, (TQuery)query));
        });
    }

    /// <summary>
    ///     Remove the data authorization middleware from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the data authorization middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder WithoutDataAuthorization(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Without<DataAuthorizationQueryMiddleware, DataAuthorizationQueryMiddlewareConfiguration>();
    }
}
