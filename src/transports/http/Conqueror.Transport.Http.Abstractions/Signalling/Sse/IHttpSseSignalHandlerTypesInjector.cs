using System;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpSseSignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    /// <summary>
    ///     Helper method to be able to access the signal types as generic parameters while only
    ///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injector that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Create<TResult>(IHttpSseSignalTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class HttpSseSignalHandlerTypesInjector<TSignal, TIHandler, THandler> : IHttpSseSignalHandlerTypesInjector
    where TSignal : class, IHttpSseSignal<TSignal>
    where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
    where THandler : class, TIHandler
{
    public static readonly HttpSseSignalHandlerTypesInjector<TSignal, TIHandler, THandler> Default = new();

    public Type SignalType { get; } = typeof(TSignal);

    public TResult Create<TResult>(IHttpSseSignalTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TSignal, TIHandler, THandler>();
}

/// <summary>
///     Helper interface to be able to access the signal types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpSseSignalTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler, THandler>()
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler;
}
