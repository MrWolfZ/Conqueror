using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public abstract class SignalMiddlewareContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public abstract TSignal Signal { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract SignalTransportType TransportType { get; }

    public abstract Task Next(TSignal signal, CancellationToken cancellationToken);
}
