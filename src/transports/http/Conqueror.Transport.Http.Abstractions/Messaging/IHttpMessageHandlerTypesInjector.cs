namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpMessageHandlerTypesInjector : IMessageHandlerTypesInjector
{
    void ConfigureHttpReceiver(IHttpMessageReceiver receiver);

    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injector that should be called with the generic type parameters</param>
    /// <param name="arg">The argument to pass to the injectable</param>
    /// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Inject<TArg, TResult>(IHttpMessageTypesInjectable<TArg, TResult> injectable, TArg arg);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(
    Action<IHttpMessageReceiver>? configureReceiver
) : IHttpMessageHandlerTypesInjector
    where TMessage : class, IHttpMessage<TMessage, TResponse>
    where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
{
    public Type MessageType { get; } = typeof(TMessage);

    public void ConfigureHttpReceiver(IHttpMessageReceiver receiver)
    {
        // can be null for delegate handlers
        configureReceiver?.Invoke(receiver);
    }

    public TResult Inject<TArg, TResult>(IHttpMessageTypesInjectable<TArg, TResult> injectable, TArg arg) =>
        injectable.WithInjectedTypes<TMessage, TResponse, TIHandler>(arg);
}

/// <summary>
///     Helper interface to be able to access the message and response types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "order makes sense here"
)]
internal interface IHttpMessageTypesInjectable<in TArg, out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse, TIHandler>(TArg arg)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>;
}
