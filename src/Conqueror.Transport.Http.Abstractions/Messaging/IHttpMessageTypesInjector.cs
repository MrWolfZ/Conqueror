using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessageTypesInjector : IMessageTypesInjector
{
    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectorWithResult">The injector that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjector<TResult> injectorWithResult);
}

/// <summary>
///     Helper interface to be able to access the message and response types as generic parameters while only
///     having a generic reference to the generated handler interface type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TResult">The type of result the factory will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessageTypesInjector<out TResult>
{
    TResult WithInjectedTypes<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TMessage,
        TResponse>()
        where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class HttpMessageTypesInjector<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    TMessage,
    TResponse> : IHttpMessageTypesInjector
    where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
{
    public static readonly HttpMessageTypesInjector<TMessage, TResponse> Default = new();

    public Type ConstraintType => typeof(IHttpMessage);

    public TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjector<TResult> injectorWithResult)
        => injectorWithResult.WithInjectedTypes<TMessage, TResponse>();
}
