using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal delegate Task<TResponse> MessageMiddlewareNext<in TMessage, TResponse>(TMessage message, CancellationToken cancellationToken);

internal sealed class DefaultMessageMiddlewareContext<TMessage, TResponse>(
    TMessage message,
    MessageMiddlewareNext<TMessage, TResponse> next,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    MessageTransportType transportType,
    CancellationToken cancellationToken)
    : MessageMiddlewareContext<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    public override TMessage Message { get; } = message;

    public override bool HasUnitResponse { get; } = typeof(TResponse) == typeof(UnitMessageResponse);

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override MessageTransportType TransportType { get; } = transportType;

    public override Task<TResponse> Next(TMessage message, CancellationToken cancellationToken) => next(message, cancellationToken);
}
