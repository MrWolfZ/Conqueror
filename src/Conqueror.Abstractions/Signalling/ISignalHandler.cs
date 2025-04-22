using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalHandler<in TSignal, THandler>
    where TSignal : class, ISignal<TSignal>
    where THandler : class, ISignalHandler<TSignal, THandler>;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedSignalHandler
{
    static virtual void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
        where T : class, ISignal<T>
    {
        // by default, we use an empty pipeline
    }

    static virtual void ConfigureInProcessReceiver<T>(IInProcessSignalReceiver<T> receiver)
        where T : class, ISignal<T>
    {
        // by default, we enable the in-process receiver for all signal types
        receiver.Enable();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedSignalHandler<in TSignal, THandler>
    : ISignalHandler<TSignal, THandler>, IGeneratedSignalHandler
    where TSignal : class, ISignal<TSignal>
    where THandler : class, ISignalHandler<TSignal, THandler>
{
    static abstract Task Invoke(THandler handler, TSignal signal, CancellationToken cancellationToken);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class GeneratedSignalHandlerAdapter<TSignal, THandler, TAdapter> : IConfigurableSignalHandler<TSignal, THandler>
    where TSignal : class, ISignal<TSignal>
    where THandler : class, ISignalHandler<TSignal, THandler>
    where TAdapter : GeneratedSignalHandlerAdapter<TSignal, THandler, TAdapter>, THandler, new()
{
    internal ISignalDispatcher<TSignal> Dispatcher { get; init; } = null!;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task Handle(TSignal signal, CancellationToken cancellationToken = default)
        => Dispatcher.Dispatch(signal, cancellationToken);

    public THandler WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline)
        => new TAdapter { Dispatcher = Dispatcher.WithPipeline(configurePipeline) };

    public THandler WithPublisher(ConfigureSignalPublisher<TSignal> configurePublisher)
        => new TAdapter { Dispatcher = Dispatcher.WithPublisher(configurePublisher) };

    public THandler WithPublisher(ConfigureSignalPublisherAsync<TSignal> configurePublisher)
        => new TAdapter { Dispatcher = Dispatcher.WithPublisher(configurePublisher) };
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IConfigurableSignalHandler<TSignal, THandler> : ISignalHandler<TSignal, THandler>
    where TSignal : class, ISignal<TSignal>
    where THandler : class, ISignalHandler<TSignal, THandler>
{
    THandler WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline);

    THandler WithPublisher(ConfigureSignalPublisher<TSignal> configurePublisher);

    THandler WithPublisher(ConfigureSignalPublisherAsync<TSignal> configurePublisher);
}
