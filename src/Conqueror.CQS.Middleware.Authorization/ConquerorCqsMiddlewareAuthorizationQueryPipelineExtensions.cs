using System.Threading.Tasks;
using Conqueror.CQS.Middleware.Authorization;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IQueryPipeline{TQuery,TResponse}" /> to add, configure, or remove authorization functionality.
/// </summary>
public static class ConquerorCqsMiddlewareAuthorizationQueryPipelineExtensions
{
    /// <summary>
    ///     Perform a query type authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the query type, then a <see cref="ConquerorOperationTypeAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow queries which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the query type authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking query type authorization</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> UseQueryTypeAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                                 ConquerorOperationTypeAuthorizationCheckAsync authorizationCheck)
        where TQuery : class
    {
        return pipeline.Use(new OperationTypeAuthorizationQueryMiddleware<TQuery, TResponse>(pipeline.ServiceProvider.GetRequiredService<IConquerorAuthenticationContext>())
        {
            Configuration = new(authorizationCheck),
        });
    }

    /// <summary>
    ///     Perform a query type authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the query type, then a <see cref="ConquerorOperationTypeAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow queries which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the query type authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking query type authorization</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> UseQueryTypeAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                                 ConquerorOperationTypeAuthorizationCheck authorizationCheck)
        where TQuery : class
    {
        return pipeline.UseQueryTypeAuthorization((principal, operationType) => Task.FromResult(authorizationCheck(principal, operationType)));
    }

    /// <summary>
    ///     Remove the <see cref="OperationTypeAuthorizationQueryMiddleware{TQuery,TResponse}" /> from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the query type authorization middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> WithoutQueryTypeAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Without<OperationTypeAuthorizationQueryMiddleware<TQuery, TResponse>>();
    }

    /// <summary>
    ///     Enable payload authorization functionality for a query pipeline. By default, this middleware will not perform
    ///     any authorization checks. Use
    ///     <see
    ///         cref="AddPayloadAuthorizationCheck{TQuery, TResponse}(IQueryPipeline{TQuery, TResponse},Conqueror.ConquerorOperationPayloadAuthorizationCheckAsync{TQuery})" />
    ///     to add
    ///     checks.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the payload authorization middleware to</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> UsePayloadAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use(new PayloadAuthorizationQueryMiddleware<TQuery, TResponse>(pipeline.ServiceProvider.GetRequiredService<IConquerorAuthenticationContext>()));
    }

    /// <summary>
    ///     Perform a payload authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the query, then a <see cref="ConquerorOperationPayloadAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow queries which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the payload authorization check to</param>
    /// <param name="authorizationCheck">The delegate to use for checking payload authorization</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> AddPayloadAuthorizationCheck<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                                    ConquerorOperationPayloadAuthorizationCheckAsync<TQuery> authorizationCheck)
        where TQuery : class
    {
        return pipeline.Configure<PayloadAuthorizationQueryMiddleware<TQuery, TResponse>>(m => m.Configuration.AuthorizationChecks.Add(authorizationCheck));
    }

    /// <summary>
    ///     Perform a payload authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the query, then a <see cref="ConquerorOperationPayloadAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow queries which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The query pipeline to add the payload authorization check to</param>
    /// <param name="authorizationCheck">The delegate to use for checking payload authorization</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> AddPayloadAuthorizationCheck<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                                    ConquerorOperationPayloadAuthorizationCheck<TQuery> authorizationCheck)
        where TQuery : class
    {
        return pipeline.AddPayloadAuthorizationCheck((principal, query) => Task.FromResult(authorizationCheck(principal, query)));
    }

    /// <summary>
    ///     Remove the payload authorization middleware from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the payload authorization middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipeline<TQuery, TResponse> WithoutPayloadAuthorization<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Without<PayloadAuthorizationQueryMiddleware<TQuery, TResponse>>();
    }
}
