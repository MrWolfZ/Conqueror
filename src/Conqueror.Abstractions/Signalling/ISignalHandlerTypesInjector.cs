using System;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Base interface for transports to be able to get an injector that works
///     with their specific constraint interface.
/// </summary>
public interface ISignalHandlerTypesInjector
{
    Type SignalType { get; }
}

internal interface ICoreSignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    Delegate? ConfigurePipeline { get; }

    void ConfigureInProcessReceiver(IInProcessSignalReceiver receiver);

    /// <summary>
    ///     Helper method to be able to access the signal types as generic parameters while only
    ///     having a generic reference to the signal type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <param name="arg">The argument to pass to the injectable</param>
    /// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Inject<TArg, TResult>(ICoreSignalHandlerTypesInjectable<TArg, TResult> injectable, TArg arg);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy>(
    Delegate? configurePipeline,
    Action<IInProcessSignalReceiver>? configureInProcessReceiver)
    : ICoreSignalHandlerTypesInjector
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler, TProxy>
    where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
{
    public Type SignalType { get; } = typeof(TSignal);

    public Delegate? ConfigurePipeline { get; } = configurePipeline;

    public void ConfigureInProcessReceiver(IInProcessSignalReceiver receiver)
    {
        // will be null for delegate handlers
        configureInProcessReceiver?.Invoke(receiver);
    }

    public TResult Inject<TArg, TResult>(ICoreSignalHandlerTypesInjectable<TArg, TResult> injectable, TArg arg)
        => injectable.WithInjectedTypes<TSignal, TIHandler, TProxy>(arg);
}

/// <summary>
///     Helper interface to be able to access the signal types as generic parameters while only
///     having a generic reference to a signal handler type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface ICoreSignalHandlerTypesInjectable<in TArg, out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler, TProxy>(TArg arg)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler, TProxy>
        where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new();
}
