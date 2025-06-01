using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Messaging;

internal sealed class MessageHandlerRegistry(IEnumerable<MessageHandlerRegistration> registrations) : IMessageHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, List<IMessageReceiverHandlerInvoker>> invokersByInjectorType = new();
    private readonly ConcurrentDictionary<(Type MessageType, Type InjectorType), IMessageReceiverHandlerInvoker?> invokerByMessageAndInjectorType = new();
    private readonly Dictionary<Type, MessageHandlerRegistration> registrationByMessageType = registrations.ToDictionary(r => r.MessageType);

    public IMessageReceiverHandlerInvoker<TTypesInjector>? GetReceiverHandlerInvoker<TMessage, TResponse, TTypesInjector>()
        where TMessage : class, IMessage<TMessage, TResponse>
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        var key = (typeof(TMessage), typeof(TTypesInjector));

        // performance optimization: we do not use `GetOrAdd` here to save on the allocation of the delegate
        if (invokerByMessageAndInjectorType.TryGetValue(key, out var invoker))
        {
            return invoker as IMessageReceiverHandlerInvoker<TTypesInjector>;
        }

        invokerByMessageAndInjectorType[key] = GetInvokerForMessageAndInjectorType<TMessage, TTypesInjector>();
        return invokerByMessageAndInjectorType[key] as IMessageReceiverHandlerInvoker<TTypesInjector>;
    }

    public IReadOnlyCollection<IMessageReceiverHandlerInvoker<TTypesInjector>> GetReceiverHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        return invokersByInjectorType.GetOrAdd(typeof(TTypesInjector),
                                               _ => [..PopulateMessageInvokersForReceiver<TTypesInjector>()])
                                     .OfType<IMessageReceiverHandlerInvoker<TTypesInjector>>()
                                     .ToList();
    }

    private List<IMessageReceiverHandlerInvoker> PopulateMessageInvokersForReceiver<TTypesInjector>()
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        var invokers = from r in registrationByMessageType.Values
                       let typesInjector = r.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault(i => i.MessageType == r.MessageType)
                       where typesInjector is not null
                       select (IMessageReceiverHandlerInvoker)new MessageReceiverHandlerInvoker<TTypesInjector>(r, typesInjector);

        return invokers.ToList();
    }

    private MessageReceiverHandlerInvoker<TTypesInjector>? GetInvokerForMessageAndInjectorType<TMessage, TTypesInjector>()
        where TTypesInjector : class, IMessageHandlerTypesInjector
    {
        var registration = registrationByMessageType.GetValueOrDefault(typeof(TMessage));

        if (registration is null)
        {
            return null;
        }

        var typesInjector = registration.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault(i => i.MessageType == registration.MessageType);
        return typesInjector is null ? null : new MessageReceiverHandlerInvoker<TTypesInjector>(registration, typesInjector);
    }
}

internal sealed record MessageHandlerRegistration(
    Type MessageType,
    Type ResponseType,
    Type? HandlerType,
    Delegate? HandlerFn,
    IMessageHandlerInvoker HandlerInvoker,
    IReadOnlyCollection<IMessageHandlerTypesInjector> TypeInjectors);
