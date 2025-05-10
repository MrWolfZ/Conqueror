using System;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpMessageHandlerTypesInjector : IMessageHandlerTypesInjector
{
    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injector that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Create<TResult>(IHttpMessageTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, THandler> : IHttpMessageHandlerTypesInjector
    where TMessage : class, IHttpMessage<TMessage, TResponse>
    where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    where THandler : class, TIHandler
{
    public static readonly HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, THandler> Default = new();

    public Type MessageType { get; } = typeof(TMessage);

    public TResult Create<TResult>(IHttpMessageTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TMessage, TResponse, TIHandler, THandler>();
}

/// <summary>
///     Helper interface to be able to access the message and response types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IHttpMessageTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse, TIHandler, THandler>()
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler;
}
