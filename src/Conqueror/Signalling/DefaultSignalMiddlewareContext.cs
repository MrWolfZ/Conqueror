using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal delegate Task SignalMiddlewareNext<in TSignal>(TSignal signal, CancellationToken cancellationToken);

internal sealed class DefaultSignalMiddlewareContext<TSignal>(
    TSignal signal,
    SignalMiddlewareNext<TSignal> next,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    SignalTransportType transportType,
    CancellationToken cancellationToken)
    : SignalMiddlewareContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public override TSignal Signal { get; } = signal;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override SignalTransportType TransportType { get; } = transportType;

    public override Task Next(TSignal signal, CancellationToken cancellationToken) => next(signal, cancellationToken);
}
