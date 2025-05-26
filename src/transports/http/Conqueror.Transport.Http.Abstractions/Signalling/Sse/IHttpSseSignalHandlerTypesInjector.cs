using System;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpSseSignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    void ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver);

    /// <summary>
    ///     Helper method to be able to access the signal types as generic parameters while only
    ///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injector that should be called with the generic type parameters</param>
    /// <param name="arg">The argument to pass to the injectable</param>
    /// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Inject<TArg, TResult>(IHttpSseSignalTypesInjectable<TArg, TResult> injectable, TArg arg);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class HttpSseSignalHandlerTypesInjector<TSignal, TIHandler>(
    Action<IHttpSseSignalReceiver> configureReceiver)
    : IHttpSseSignalHandlerTypesInjector
    where TSignal : class, IHttpSseSignal<TSignal>
    where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
{
    public Type SignalType { get; } = typeof(TSignal);

    public void ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver) => configureReceiver(receiver);

    public TResult Inject<TArg, TResult>(IHttpSseSignalTypesInjectable<TArg, TResult> injectable, TArg arg)
        => injectable.WithInjectedTypes<TSignal, TIHandler>(arg);
}

/// <summary>
///     Helper interface to be able to access the signal types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpSseSignalTypesInjectable<in TArg, out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler>(TArg arg)
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>;
}
