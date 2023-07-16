namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="FunctionalAuthorizationCommandMiddleware" />.
/// </summary>
public sealed class FunctionalAuthorizationCommandMiddlewareConfiguration
{
    public FunctionalAuthorizationCommandMiddlewareConfiguration(ConquerorFunctionalAuthorizationCheck authorizationCheck)
    {
        AuthorizationCheck = authorizationCheck;
    }

    /// <summary>
    ///     The delegate to use for checking operation authorization. 
    /// </summary>
    public ConquerorFunctionalAuthorizationCheck AuthorizationCheck { get; set; }
}
