using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal interface ISignalDispatcher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    Task Dispatch(TSignal signal, CancellationToken cancellationToken);

    ISignalDispatcher<TSignal> WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline);

    ISignalDispatcher<TSignal> WithPublisher(ConfigureSignalPublisher<TSignal> configurePublisher);

    ISignalDispatcher<TSignal> WithPublisher(ConfigureSignalPublisherAsync<TSignal> configurePublisher);
}
