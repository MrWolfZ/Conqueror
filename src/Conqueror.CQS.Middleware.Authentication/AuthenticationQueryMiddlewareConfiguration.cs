namespace Conqueror.CQS.Middleware.Authentication;

/// <summary>
///     The configuration options for <see cref="AuthenticationQueryMiddleware" />.
/// </summary>
public sealed class AuthenticationQueryMiddlewareConfiguration
{
    /// <summary>
    ///     Configure whether the current pipeline execution requires an authenticated principal.
    /// </summary>
    public bool RequireAuthenticatedPrincipal { get; set; }
}
