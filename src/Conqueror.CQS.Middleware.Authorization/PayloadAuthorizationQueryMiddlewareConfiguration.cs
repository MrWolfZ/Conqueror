using System.Collections.Generic;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="PayloadAuthorizationQueryMiddleware" />.
/// </summary>
public sealed class PayloadAuthorizationQueryMiddlewareConfiguration
{
    /// <summary>
    ///     The delegate to use for checking payload authorization.
    /// </summary>
    public List<ConquerorOperationPayloadAuthorizationCheck<object>> AuthorizationChecks { get; } = new();
}
