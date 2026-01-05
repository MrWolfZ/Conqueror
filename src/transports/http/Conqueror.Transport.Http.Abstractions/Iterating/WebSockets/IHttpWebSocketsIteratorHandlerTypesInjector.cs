namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpWebSocketsIteratorHandlerTypesInjector : IIteratorHandlerTypesInjector
{
    void ConfigureHttpWebSocketsServer(IHttpWebSocketsIteratorServer server);

    /// <summary>
    ///     Helper method to be able to access the iterator and item types as generic parameters while only
    ///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injector that should be called with the generic type parameters</param>
    /// <param name="arg">The argument to pass to the injectable</param>
    /// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Inject<TArg, TResult>(IHttpWebSocketsIteratorTypesInjectable<TArg, TResult> injectable, TArg arg);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class HttpWebSocketsIteratorHandlerTypesInjector<TIterator, TItem, TIHandler>(
    Action<IHttpWebSocketsIteratorServer> configureServer
) : IHttpWebSocketsIteratorHandlerTypesInjector
    where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
    where TIHandler : class, IHttpWebSocketsIteratorHandler<TIterator, TItem, TIHandler>
{
    public Type IteratorType { get; } = typeof(TIterator);

    public void ConfigureHttpWebSocketsServer(IHttpWebSocketsIteratorServer server) => configureServer(server);

    public TResult Inject<TArg, TResult>(IHttpWebSocketsIteratorTypesInjectable<TArg, TResult> injectable, TArg arg) =>
        injectable.WithInjectedTypes<TIterator, TItem, TIHandler>(arg);
}

/// <summary>
///     Helper interface to be able to access the iterator and item types as generic parameters while only
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
internal interface IHttpWebSocketsIteratorTypesInjectable<in TArg, out TResult>
{
    TResult WithInjectedTypes<TIterator, TItem, TIHandler>(TArg arg)
        where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
        where TIHandler : class, IHttpWebSocketsIteratorHandler<TIterator, TItem, TIHandler>;
}
