using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate ISignalPublisher<TSignal> ConfigureSignalPublisher<TSignal>(
    ISignalPublisherBuilder<TSignal> builder)
    where TSignal : class, ISignal<TSignal>;

public delegate Task<ISignalPublisher<TSignal>> ConfigureSignalPublisherAsync<TSignal>(
    ISignalPublisherBuilder<TSignal> builder)
    where TSignal : class, ISignal<TSignal>;

public interface ISignalPublisher<in TSignal>
    where TSignal : class, ISignal<TSignal>
{
    string TransportTypeName { get; }

    Task Publish(TSignal signal,
                 IServiceProvider serviceProvider,
                 ConquerorContext conquerorContext,
                 CancellationToken cancellationToken);
}

public interface ISignalPublisherBuilder<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }
}
