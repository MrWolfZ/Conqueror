using System.Collections.Generic;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="PayloadAuthorizationCommandMiddleware" />.
/// </summary>
public sealed class PayloadAuthorizationCommandMiddlewareConfiguration
{
    /// <summary>
    ///     The delegate to use for checking payload authorization.
    /// </summary>
    public List<ConquerorOperationPayloadAuthorizationCheck<object>> AuthorizationChecks { get; } = new();
}
