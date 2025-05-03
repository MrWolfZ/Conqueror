using System.Collections.Generic;
using System.Threading.Tasks;

namespace Conqueror.Middleware.Authorization.Messaging;

public delegate Task<AuthorizationResult> AuthorizationCheckFn<TMessage, TResponse>(
    MessageAuthorizationContext<TMessage, TResponse> context)
    where TMessage : class, IMessage<TMessage, TResponse>;

public delegate AuthorizationResult AuthorizationCheckSyncFn<TMessage, TResponse>(
    MessageAuthorizationContext<TMessage, TResponse> context)
    where TMessage : class, IMessage<TMessage, TResponse>;

/// <summary>
///     The configuration options for <see cref="AuthorizationMessageMiddleware{TMessage,TResponse}" />.
/// </summary>
public sealed class AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public List<(string Id, AuthorizationCheckFn<TMessage, TResponse> Check)> AuthorizationChecks { get; } = [];

    /// <summary>
    ///     Add an authorization check to the configuration.
    /// </summary>
    /// <param name="id">The id of this check (can be used to modify <see cref="AuthorizationChecks" /> later)</param>
    /// <param name="check">The check to execute</param>
    /// <returns>The configuration for chaining</returns>
    public AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse> AddAuthorizationCheck(
        string id,
        AuthorizationCheckFn<TMessage, TResponse> check)
    {
        AuthorizationChecks.Add((id, check));
        return this;
    }

    /// <summary>
    ///     Add a synchronous authorization check to the configuration.
    /// </summary>
    /// <param name="id">The id of this check (can be used to modify <see cref="AuthorizationChecks" /> later)</param>
    /// <param name="check">The check to execute</param>
    /// <returns>The configuration for chaining</returns>
    public AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse> AddAuthorizationCheck(
        string id,
        AuthorizationCheckSyncFn<TMessage, TResponse> check)
        => AddAuthorizationCheck(id, ctx => Task.FromResult(check(ctx)));

    /// <summary>
    ///     Remove <b>ALL</b> authorization checks with the given id.
    /// </summary>
    /// <param name="id">The id of the checks to remove</param>
    /// <returns>The configuration for chaining</returns>
    public AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse> RemoveAuthorizationCheck(string id)
    {
        _ = AuthorizationChecks.RemoveAll(x => x.Id == id);
        return this;
    }
}
