﻿using System.Collections.Generic;
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     This interface does not need to be added manually to user code. It is
///     generated by the source generator and is used by Conqueror APIs to infer
///     the types.
/// </summary>
/// <typeparam name="TMessage">the message type</typeparam>
/// <typeparam name="TResponse">the response type</typeparam>
public interface IMessage<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract IReadOnlyCollection<IMessageTypesInjector> TypeInjectors { get; }

    /// <summary>
    ///     This helper property can be used for type inference instead of having
    ///     to provide both the generic message and response type arguments.<br />
    ///     <br />
    ///     For example, instead of typing <code>messageClients.For&lt;MyMessage, MyMessageResponse&gt;()</code>
    ///     you can write <code>messageClients.For(MyMessage.T)</code>, which is markedly shorter.
    /// </summary>
    static abstract MessageTypes<TMessage, TResponse> T { get; }

    /// <summary>
    ///     Some transports must be able to construct an instance of this message
    ///     if it has no properties, but since this is only known to the actual
    ///     type, we cannot use the generic type constraint 'new()'. Instead, we
    ///     generate an empty instance in the source generator if the message type
    ///     does not have any properties.
    /// </summary>
    static abstract TMessage? EmptyInstance { get; }
}

/// <summary>
///     This helper class is only used for enhanced type inference.
/// </summary>
/// <typeparam name="TMessage">the message type</typeparam>
/// <typeparam name="TResponse">the response type</typeparam>
public sealed class MessageTypes<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public static readonly MessageTypes<TMessage, TResponse> Default = new();

    private MessageTypes()
    {
    }
}
