using System;

namespace Conqueror.Signalling;

internal sealed class SignalPublisherBuilder<TSignal>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext)
    : ISignalPublisherBuilder<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;
}
