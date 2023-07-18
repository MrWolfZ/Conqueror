namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="OperationTypeAuthorizationCommandMiddleware" />.
/// </summary>
public sealed class OperationTypeAuthorizationCommandMiddlewareConfiguration
{
    public OperationTypeAuthorizationCommandMiddlewareConfiguration(ConquerorOperationTypeAuthorizationCheckAsync authorizationCheck)
    {
        AuthorizationCheck = authorizationCheck;
    }

    /// <summary>
    ///     The delegate to use for checking operation type authorization. 
    /// </summary>
    public ConquerorOperationTypeAuthorizationCheckAsync AuthorizationCheck { get; set; }
}
