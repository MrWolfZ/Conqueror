using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate ISignalPublisher<TSignal> ConfigureSignalPublisher<TSignal>(
    ISignalPublisherBuilder<TSignal> builder)
    where TSignal : class, ISignal<TSignal>;

public delegate Task<ISignalPublisher<TSignal>> ConfigureSignalPublisherAsync<TSignal>(
    ISignalPublisherBuilder<TSignal> builder)
    where TSignal : class, ISignal<TSignal>;
