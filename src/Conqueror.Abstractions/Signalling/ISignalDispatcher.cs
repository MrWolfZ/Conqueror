namespace Conqueror;

internal interface ISignalDispatcher
{
    Task Dispatch<TSignal>(
        TSignal signal,
        IServiceProvider serviceProvider,
        Action<ISignalPipeline<TSignal>>? configurePipeline,
        ISignalPublisher<TSignal>? publisher,
        ConfigureSignalPublisherAsync<TSignal>? configurePublisherAsync,
        CancellationToken cancellationToken
    )
        where TSignal : class, ISignal<TSignal>;
}
