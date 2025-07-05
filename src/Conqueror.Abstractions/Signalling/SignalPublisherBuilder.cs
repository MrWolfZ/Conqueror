using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct SignalPublisherBuilder<TSignal>(IServiceProvider ServiceProvider)
    where TSignal : class, ISignal<TSignal>;
