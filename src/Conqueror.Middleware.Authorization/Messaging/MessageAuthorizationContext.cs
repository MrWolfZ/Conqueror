using System;

namespace Conqueror.Middleware.Authorization.Messaging;

public sealed class MessageAuthorizationContext<TMessage, TResponse>(
    TMessage message,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext)
    : AuthorizationContext
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public TMessage Message { get; } = message;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;
}
