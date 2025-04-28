using System;
using System.Security.Claims;
using System.Threading;

namespace Conqueror.Middleware.Authorization.Messaging;

public sealed class MessageAuthorizationContext<TMessage, TResponse>(
    TMessage message,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    ClaimsPrincipal? currentPrincipal,
    CancellationToken cancellationToken)
    : AuthorizationContext
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public TMessage Message { get; } = message;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override ClaimsPrincipal? CurrentPrincipal { get; } = currentPrincipal;

    public override CancellationToken CancellationToken { get; } = cancellationToken;
}
