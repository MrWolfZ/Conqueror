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

    static virtual void ConfigureInProcessReceiver(IInProcessSignalReceiver receiver)
    {
        // we don't configure the receiver (by default, it is enabled for all signal types)
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface ISignalHandler<TSignal, TIHandler> : ISignalHandler
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler>
{
    static virtual SignalTypes<TSignal, TIHandler> SignalTypes { get; } = new();

    static abstract Task Invoke(TIHandler handler, TSignal signal, CancellationToken cancellationToken);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISignalHandler<TSignal, TIHandler, TProxy> : ISignalHandler<TSignal, TIHandler>
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler, TProxy>
    where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateCoreTypesInjector()
        => new CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy>(null, null);

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateCoreTypesInjector<THandler>()
        where THandler : class, TIHandler
        => new CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy>(
            THandler.ConfigurePipeline<TSignal>,
            THandler.ConfigureInProcessReceiver);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SignalHandlerProxy<TSignal, TIHandler, TProxy> : ISignalHandlerProxy<TSignal, TIHandler>
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
{
    // cannot be 'required' since that would block the `new()` constraint
    internal IServiceProvider ServiceProvider { get; init; } = null!;

    internal ISignalDispatcher Dispatcher { get; init; } = null!;

    private Action<ISignalPipeline<TSignal>>? ConfigurePipeline { get; init; }

    private ConfigureSignalPublisher<TSignal>? ConfigurePublisher { get; init; }

    private ConfigureSignalPublisherAsync<TSignal>? ConfigurePublisherAsync { get; init; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task Handle(TSignal signal, CancellationToken cancellationToken = default)
        => Dispatcher.Dispatch(
            signal,
            ServiceProvider,
            ConfigurePipeline,
            publisher: null,
            ConfigurePublisher,
            ConfigurePublisherAsync,
            cancellationToken);

    public TIHandler WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline)
        => new TProxy
        {
            ServiceProvider = ServiceProvider,
            Dispatcher = Dispatcher,
            ConfigurePipeline = (Action<ISignalPipeline<TSignal>>)Delegate.Combine(ConfigurePipeline, configurePipeline),
            ConfigurePublisher = ConfigurePublisher,
            ConfigurePublisherAsync = ConfigurePublisherAsync,
        };

    public TIHandler WithTransport(ConfigureSignalPublisher<TSignal> configurePublisher)
        => new TProxy
        {
            ServiceProvider = ServiceProvider,
            Dispatcher = Dispatcher,
            ConfigurePipeline = ConfigurePipeline,
            ConfigurePublisher = configurePublisher,
            ConfigurePublisherAsync = null,
        };

    public TIHandler WithTransport(ConfigureSignalPublisherAsync<TSignal> configurePublisherAsync)
        => new TProxy
        {
            ServiceProvider = ServiceProvider,
            Dispatcher = Dispatcher,
            ConfigurePipeline = ConfigurePipeline,
            ConfigurePublisher = null,
            ConfigurePublisherAsync = configurePublisherAsync,
        };

    static IEnumerable<ISignalHandlerTypesInjector> ISignalHandler.GetTypeInjectors()
        => throw new NotSupportedException("this method should never be called on the proxy");

    static Task ISignalHandler<TSignal, TIHandler>.Invoke(TIHandler handler, TSignal signal, CancellationToken cancellationToken)
        => throw new NotSupportedException("this method should never be called on the proxy");
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface ISignalHandlerProxy<TSignal, THandler> : ISignalHandler<TSignal, THandler>
    where TSignal : class, ISignal<TSignal>
    where THandler : class, ISignalHandler<TSignal, THandler>
{
    THandler WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline);

    THandler WithTransport(ConfigureSignalPublisher<TSignal> configurePublisher);

    THandler WithTransport(ConfigureSignalPublisherAsync<TSignal> configurePublisherAsync);
}
