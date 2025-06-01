using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal interface ISignalDispatcher
{
    Task Dispatch<TSignal>(
        TSignal signal,
        IServiceProvider serviceProvider,
        Action<ISignalPipeline<TSignal>>? configurePipeline,
        ISignalPublisher<TSignal>? publisher,
        ConfigureSignalPublisher<TSignal>? configurePublisher,
        ConfigureSignalPublisherAsync<TSignal>? configurePublisherAsync,
        CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>;
}
