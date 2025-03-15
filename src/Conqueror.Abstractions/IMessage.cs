﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "The parameter is used for type checking")]
public interface IMessage<TResponse>
{
    static abstract IReadOnlyCollection<IMessageTypesInjector> TypeInjectors { get; }

    /// <summary>
    ///     This helper function is used to invoke a handler if it is not known
    ///     at the call-site whether the message has a response or not, and when
    ///     the generic constraints don't allow for casting the handler to the
    ///     correct type.
    /// </summary>
    internal static virtual Task<TResponse> CastAndInvokeHandler<TMessage>(object handler,
                                                                           TMessage message,
                                                                           CancellationToken cancellationToken = default)
        where TMessage : class, IMessage<TResponse>
    {
        return ((IMessageHandler<TMessage, TResponse>)handler).Handle(message, cancellationToken);
    }
}

public interface IMessage : IMessage<UnitMessageResponse>
{
    static async Task<UnitMessageResponse> IMessage<UnitMessageResponse>.CastAndInvokeHandler<TMessage>(object handler,
                                                                                                        TMessage message,
                                                                                                        CancellationToken cancellationToken)
    {
        await ((IMessageHandler<TMessage>)handler).Handle(message, cancellationToken).ConfigureAwait(false);
        return UnitMessageResponse.Instance;
    }
}

/// <summary>
///     This interface does not need to be added manually to user code. It is
///     generated by the source generator and is used by Conqueror APIs to infer
///     the types.
/// </summary>
/// <typeparam name="TMessage">the message type</typeparam>
/// <typeparam name="TResponse">the response type</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageTypes<out TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    /// <summary>
    ///     Some transports must be able to construct an instance of this message
    ///     if it has no properties, but since this is only known to the actual
    ///     type, we cannot use the generic type constraint 'new()'. Instead, we
    ///     generate an empty instance in the source generator if the message type
    ///     does not have any properties.
    /// </summary>
    static abstract TMessage? EmptyInstance { get; }
}
