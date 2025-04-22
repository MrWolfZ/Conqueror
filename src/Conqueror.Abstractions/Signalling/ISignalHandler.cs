using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISignalHandler
{
    /// <summary>
    ///     Implemented by source generator for each handler type. Cannot be abstract since otherwise
    ///     the generated <code>IHandler</code> types could not be used as generic arguments.
    /// </summary>
    static virtual IEnumerable<ISignalHandlerTypesInjector> GetTypeInjectors()
        => throw new NotSupportedException("this should be implemented by the source generator for each concrete handler type");

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
public interface ISignalHandler<in TSignal, in TIHandler> : ISignalHandler
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler>
{
    internal static virtual ICoreSignalHandlerTypesInjector CoreTypesInjector
        => throw new NotSupportedException("this should be implemented by the source generator for each concrete handler type");

    static virtual Task Invoke(TIHandler handler, TSignal signal, CancellationToken cancellationToken)
        => throw new NotSupportedException("this should be implemented by the source generator for each concrete handler interface type");
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISignalHandler<in TSignal, in TIHandler, TProxy> : ISignalHandler<TSignal, TIHandler>
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler, TProxy>
    where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
{
    /// <summary>
    ///     We are cheating a bit here by using <see cref="TIHandler" /> as the type parameter for the handler type
    ///     of the default types injector. This is because this property here is only used to generate publishers
    ///     with the correct interface (<see cref="ISignalPublishers" />), and we don't need the concrete handler type
    ///     there.
    /// </summary>
    static ICoreSignalHandlerTypesInjector ISignalHandler<TSignal, TIHandler>.CoreTypesInjector
        => CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy, TIHandler>.Default;

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateCoreTypesInjector<THandler>()
        where THandler : class, TIHandler
        => CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy, THandler>.Default;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SignalHandlerProxy<TSignal, TIHandler, TProxy> : ISignalHandlerProxy<TSignal, TIHandler>
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
{
    internal ISignalDispatcher<TSignal> Dispatcher { get; init; } = null!;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task Handle(TSignal signal, CancellationToken cancellationToken = default)
        => Dispatcher.Dispatch(signal, cancellationToken);

    public TIHandler WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline)
        => new TProxy { Dispatcher = Dispatcher.WithPipeline(configurePipeline) };

    public TIHandler WithPublisher(ConfigureSignalPublisher<TSignal> configurePublisher)
        => new TProxy { Dispatcher = Dispatcher.WithPublisher(configurePublisher) };

    public TIHandler WithPublisher(ConfigureSignalPublisherAsync<TSignal> configurePublisher)
        => new TProxy { Dispatcher = Dispatcher.WithPublisher(configurePublisher) };
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface ISignalHandlerProxy<TSignal, THandler> : ISignalHandler<TSignal, THandler>
    where TSignal : class, ISignal<TSignal>
    where THandler : class, ISignalHandler<TSignal, THandler>
{
    THandler WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline);

    THandler WithPublisher(ConfigureSignalPublisher<TSignal> configurePublisher);

    THandler WithPublisher(ConfigureSignalPublisherAsync<TSignal> configurePublisher);
}
