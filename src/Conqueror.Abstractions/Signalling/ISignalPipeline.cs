#pragma warning disable CA1034

namespace Conqueror;

public delegate Task SignalMiddlewareFn<TSignal>(SignalMiddlewareContext<TSignal> context)
    where TSignal : class, ISignal<TSignal>;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "naming is intentional")]
public interface ISignalPipeline<TSignal> : IReadOnlyCollection<ISignalMiddleware<TSignal>>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The type of the handler this pipeline is being built for. Is <see langword="null" /> for
    ///     delegate handlers or when the pipeline is being built for a publisher.
    /// </summary>
    Type? HandlerType { get; }

    IServiceProvider ServiceProvider { get; }

    ISignalPipeline<TSignal> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ISignalMiddleware<TSignal>;

    ISignalPipeline<TSignal> Use(SignalMiddlewareFn<TSignal> middlewareFn);

    ISignalPipeline<TSignal> UseWhen(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        Action<ISignalPipeline<TSignal>> configureConditionalPipeline
    );

    ISignalPipeline<TSignal> Without<TMiddleware>()
        where TMiddleware : ISignalMiddleware<TSignal>;

    ISignalPipeline<TSignal> Configure<TMiddleware>(Action<TMiddleware> configureFn)
        where TMiddleware : ISignalMiddleware<TSignal>;
}
