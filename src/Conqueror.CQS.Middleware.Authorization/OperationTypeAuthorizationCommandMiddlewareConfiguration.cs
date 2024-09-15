namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="OperationTypeAuthorizationCommandMiddleware{TCommand,TResponse}" />.
/// </summary>
public sealed class OperationTypeAuthorizationCommandMiddlewareConfiguration(ConquerorOperationTypeAuthorizationCheckAsync authorizationCheck)
{
    /// <summary>
    ///     The delegate to use for checking operation type authorization.
    /// </summary>
    public ConquerorOperationTypeAuthorizationCheckAsync AuthorizationCheck { get; set; } = authorizationCheck;
}
