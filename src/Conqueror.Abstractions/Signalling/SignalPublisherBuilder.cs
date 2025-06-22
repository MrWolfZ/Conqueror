using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct SignalPublisherBuilder<TSignal>(
    IServiceProvider ServiceProvider,
    ConquerorContext ConquerorContext)
    where TSignal : class, ISignal<TSignal>;
