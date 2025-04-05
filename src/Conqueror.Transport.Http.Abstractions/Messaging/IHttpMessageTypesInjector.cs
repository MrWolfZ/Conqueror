using System;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessageTypesInjector : IMessageTypesInjector
{
    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injector that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjectable<TResult> injectable);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class HttpMessageTypesInjector<TMessage, TResponse> : IHttpMessageTypesInjector
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public static readonly HttpMessageTypesInjector<TMessage, TResponse> Default = new();

    public Type ConstraintType => typeof(IHttpMessage);

    public TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TMessage, TResponse>();
}

/// <summary>
///     Helper interface to be able to access the message and response types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessageTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse>()
        where TMessage : class, IHttpMessage<TMessage, TResponse>;
}
