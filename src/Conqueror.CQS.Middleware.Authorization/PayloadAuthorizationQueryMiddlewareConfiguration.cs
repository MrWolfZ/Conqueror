using System.Collections.Generic;

namespace Conqueror.CQS.Middleware.Authorization;

/// <summary>
///     The configuration options for <see cref="PayloadAuthorizationQueryMiddleware{TQuery,TResponse}" />.
/// </summary>
public sealed class PayloadAuthorizationQueryMiddlewareConfiguration<TQuery>
    where TQuery : class
{
    /// <summary>
    ///     The delegate to use for checking payload authorization.
    /// </summary>
    public List<ConquerorOperationPayloadAuthorizationCheckAsync<TQuery>> AuthorizationChecks { get; } = [];
}
