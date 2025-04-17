using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Middleware.Authorization.Messaging;

/// <summary>
///     The configuration options for <see cref="AuthorizationMessageMiddleware{TMessage,TResponse}" />.
/// </summary>
public sealed class AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    internal List<Func<MessageAuthorizationContext<TMessage, TResponse>, CancellationToken, Task<AuthorizationResult>>> AuthorizationChecks { get; } = new();

    public AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse> AddAuthorizationCheck(
        Func<MessageAuthorizationContext<TMessage, TResponse>, CancellationToken, Task<AuthorizationResult>> check)
    {
        AuthorizationChecks.Add(check);
        return this;
    }

    public AuthorizationMessageMiddlewareConfiguration<TMessage, TResponse> AddAuthorizationCheck(
        Func<MessageAuthorizationContext<TMessage, TResponse>, AuthorizationResult> check)
    {
        AuthorizationChecks.Add((ctx, _) => Task.FromResult(check(ctx)));
        return this;
    }
}
