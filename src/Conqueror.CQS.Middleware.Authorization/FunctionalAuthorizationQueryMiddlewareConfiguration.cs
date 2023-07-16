namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="FunctionalAuthorizationQueryMiddleware" />.
/// </summary>
public sealed class FunctionalAuthorizationQueryMiddlewareConfiguration
{
    public FunctionalAuthorizationQueryMiddlewareConfiguration(ConquerorFunctionalAuthorizationCheck authorizationCheck)
    {
        AuthorizationCheck = authorizationCheck;
    }

    /// <summary>
    ///     The delegate to use for checking operation authorization. 
    /// </summary>
    public ConquerorFunctionalAuthorizationCheck AuthorizationCheck { get; set; }
}
