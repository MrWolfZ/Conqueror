using Conqueror.CQS.Middleware.Authorization;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ICommandPipelineBuilder" /> to add, configure, or remove functional authorization functionality.
/// </summary>
public static class ConquerorCqsMiddlewareAuthorizationCommandPipelineBuilderExtensions
{
    /// <summary>
    ///     Perform a functional authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.GetCurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the command, then a <see cref="ConquerorFunctionalAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow commands which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the functional authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking operation authorization</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder UseFunctionalAuthorization(this ICommandPipelineBuilder pipeline, ConquerorFunctionalAuthorizationCheck authorizationCheck)
    {
        return pipeline.Use<FunctionalAuthorizationCommandMiddleware, FunctionalAuthorizationCommandMiddlewareConfiguration>(new(authorizationCheck));
    }

    /// <summary>
    ///     Remove the functional authorization middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the functional authorization middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder WithoutFunctionalAuthorization(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Without<FunctionalAuthorizationCommandMiddleware, FunctionalAuthorizationCommandMiddlewareConfiguration>();
    }

    /// <summary>
    ///     Enable data authorization functionality for a pipeline. By default, this middleware will not perform
    ///     any authorization checks. Use <see cref="AddDataAuthorizationCheck{TCommand}" /> to add checks.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the data authorization middleware to</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder UseDataAuthorization(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAuthorizationCommandMiddleware, DataAuthorizationCommandMiddlewareConfiguration>(new());
    }

    /// <summary>
    ///     Perform a data authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.GetCurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the command, then a <see cref="ConquerorDataAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow commands which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the functional authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking operation authorization</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder AddDataAuthorizationCheck<TCommand>(this ICommandPipelineBuilder pipeline, ConquerorDataAuthorizationCheck<TCommand> authorizationCheck)
        where TCommand : class
    {
        return pipeline.Configure<DataAuthorizationCommandMiddleware, DataAuthorizationCommandMiddlewareConfiguration>(o =>
        {
            // we assume that the user passed in a compatible command type, so we perform a simple cast
            o.AuthorizationChecks.Add((principal, command) => authorizationCheck(principal, (TCommand)command));
        });
    }

    /// <summary>
    ///     Remove the data authorization middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the data authorization middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder WithoutDataAuthorization(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Without<DataAuthorizationCommandMiddleware, DataAuthorizationCommandMiddlewareConfiguration>();
    }
}
