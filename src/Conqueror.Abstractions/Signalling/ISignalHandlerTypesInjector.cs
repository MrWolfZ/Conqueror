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

public interface ICoreSignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    TResult Create<TResult>(ICoreSignalHandlerTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy, THandler> : ICoreSignalHandlerTypesInjector
    where TSignal : class, ISignal<TSignal>
    where TIHandler : class, ISignalHandler<TSignal, TIHandler, TProxy>
    where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
    where THandler : class, TIHandler
{
    public static readonly CoreSignalHandlerTypesInjector<TSignal, TIHandler, TProxy, THandler> Default = new();

    public Type SignalType { get; } = typeof(TSignal);

    /// <summary>
    ///     Helper method to be able to access the signal types as generic parameters while only
    ///     having a generic reference to the signal type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    public TResult Create<TResult>(ICoreSignalHandlerTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>();
}

/// <summary>
///     Helper interface to be able to access the signal types as generic parameters while only
///     having a generic reference to a signal handler type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ICoreSignalHandlerTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler, TProxy>
        where TProxy : SignalHandlerProxy<TSignal, TIHandler, TProxy>, TIHandler, new()
        where THandler : class, TIHandler;
}
