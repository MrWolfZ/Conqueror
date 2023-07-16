using System.Collections.Generic;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="DataAuthorizationQueryMiddleware" />.
/// </summary>
public sealed class DataAuthorizationQueryMiddlewareConfiguration
{
    /// <summary>
    ///     The delegate to use for checking operation authorization.
    /// </summary>
    public List<ConquerorDataAuthorizationCheck<object>> AuthorizationChecks { get; } = new();
}
