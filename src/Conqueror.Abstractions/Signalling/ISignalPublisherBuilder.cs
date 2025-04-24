using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalPublisherBuilder<in TSignal>
    where TSignal : class, ISignal<TSignal>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }
}
