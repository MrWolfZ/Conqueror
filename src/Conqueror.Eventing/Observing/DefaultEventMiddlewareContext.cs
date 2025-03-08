using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal delegate Task EventMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

internal sealed class DefaultEventMiddlewareContext<TEvent>(
    TEvent evt,
    EventMiddlewareNext<TEvent> next,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    EventTransportType transportType,
    CancellationToken cancellationToken)
    : EventMiddlewareContext<TEvent>
    where TEvent : class
{
    public override TEvent Event { get; } = evt;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override EventTransportType TransportType { get; } = transportType;

    public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
}
