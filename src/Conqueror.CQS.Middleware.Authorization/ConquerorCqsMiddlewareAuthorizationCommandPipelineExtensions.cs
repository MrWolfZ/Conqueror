using System.Threading.Tasks;
using Conqueror.CQS.Middleware.Authorization;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ICommandPipeline" /> to add, configure, or remove authorization functionality.
/// </summary>
public static class ConquerorCqsMiddlewareAuthorizationCommandPipelineExtensions
{
    /// <summary>
    ///     Perform a command type authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the command type, then a <see cref="ConquerorOperationTypeAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow commands which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the command type authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking command type authorization</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> UseCommandTypeAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                                         ConquerorOperationTypeAuthorizationCheckAsync authorizationCheck)
        where TCommand : class
    {
        return pipeline.Use<OperationTypeAuthorizationCommandMiddleware, OperationTypeAuthorizationCommandMiddlewareConfiguration>(new(authorizationCheck));
    }

    /// <summary>
    ///     Perform a command type authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the command type, then a <see cref="ConquerorOperationTypeAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow commands which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the command type authorization middleware to</param>
    /// <param name="authorizationCheck">The delegate to use for checking command type authorization</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> UseCommandTypeAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                                         ConquerorOperationTypeAuthorizationCheck authorizationCheck)
        where TCommand : class
    {
        return pipeline.UseCommandTypeAuthorization((principal, operationType) => Task.FromResult(authorizationCheck(principal, operationType)));
    }

    /// <summary>
    ///     Remove the <see cref="OperationTypeAuthorizationCommandMiddleware" /> from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the command type authorization middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> WithoutCommandTypeAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Without<OperationTypeAuthorizationCommandMiddleware, OperationTypeAuthorizationCommandMiddlewareConfiguration>();
    }

    /// <summary>
    ///     Enable payload authorization functionality for a command pipeline. By default, this middleware will not perform
    ///     any authorization checks. Use <see cref="AddPayloadAuthorizationCheck{TCommand, TResponse}(ICommandPipeline,Conqueror.ConquerorOperationPayloadAuthorizationCheckAsync{TCommand})" /> to add checks.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the payload authorization middleware to</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> UsePayloadAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use<PayloadAuthorizationCommandMiddleware, PayloadAuthorizationCommandMiddlewareConfiguration>(new());
    }

    /// <summary>
    ///     Perform a payload authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the command, then a <see cref="ConquerorOperationPayloadAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow commands which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the payload authorization check to</param>
    /// <param name="authorizationCheck">The delegate to use for checking payload authorization</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> AddPayloadAuthorizationCheck<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                                          ConquerorOperationPayloadAuthorizationCheckAsync<TCommand> authorizationCheck)
        where TCommand : class
    {
        return pipeline.Configure<PayloadAuthorizationCommandMiddleware, PayloadAuthorizationCommandMiddlewareConfiguration>(o =>
        {
            // we assume that the user passed in a compatible command type, so we perform a simple cast
            o.AuthorizationChecks.Add((principal, command) => authorizationCheck(principal, (TCommand)command));
        });
    }

    /// <summary>
    ///     Perform a payload authorization check for the current principal (see
    ///     <see cref="IConquerorAuthenticationContext.CurrentPrincipal" />) when executing the pipeline. If the principal
    ///     is not authorized for the command, then a <see cref="ConquerorOperationPayloadAuthorizationFailedException" />
    ///     will be thrown during pipeline execution.<br />
    ///     <br />
    ///     Note that if there is no principal present or the principal is not authenticated, then the authorization check is
    ///     skipped. This is in order to allow commands which allow anonymous access to execute in a shared pipeline which has
    ///     this authorization check in place. To enforce the presence of an authenticated principal, please use the
    ///     configuration features of the Conqueror authentication middleware.
    /// </summary>
    /// <param name="pipeline">The command pipeline to add the payload authorization check to</param>
    /// <param name="authorizationCheck">The delegate to use for checking payload authorization</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> AddPayloadAuthorizationCheck<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                                          ConquerorOperationPayloadAuthorizationCheck<TCommand> authorizationCheck)
        where TCommand : class
    {
        return pipeline.AddPayloadAuthorizationCheck((principal, command) => Task.FromResult(authorizationCheck(principal, command)));
    }

    /// <summary>
    ///     Remove the payload authorization middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the payload authorization middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipeline<TCommand, TResponse> WithoutPayloadAuthorization<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Without<PayloadAuthorizationCommandMiddleware, PayloadAuthorizationCommandMiddlewareConfiguration>();
    }
}
