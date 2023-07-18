namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="OperationTypeAuthorizationQueryMiddleware" />.
/// </summary>
public sealed class OperationTypeAuthorizationQueryMiddlewareConfiguration
{
    public OperationTypeAuthorizationQueryMiddlewareConfiguration(ConquerorOperationTypeAuthorizationCheckAsync authorizationCheck)
    {
        AuthorizationCheck = authorizationCheck;
    }

    /// <summary>
    ///     The delegate to use for checking operation type authorization. 
    /// </summary>
    public ConquerorOperationTypeAuthorizationCheckAsync AuthorizationCheck { get; set; }
}
