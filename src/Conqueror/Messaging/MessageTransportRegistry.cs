using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Messaging;

internal sealed class MessageTransportRegistry : IMessageTransportRegistry
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(MessageHandlerRegistration Registration, IMessageTypesInjector? TypeInjector)>> messageTypesByInterface = new();
    private readonly Dictionary<Type, MessageHandlerRegistration> registrationByMessageType;

    public MessageTransportRegistry(IEnumerable<MessageHandlerRegistration> registrations)
    {
        registrationByMessageType = registrations.ToDictionary(r => r.MessageType);
    }

    public IReadOnlyCollection<(Type MessageType, Type ResponseType, IMessageTypesInjector? TypeInjector)> GetMessageTypesForTransportInterface<TInterface>()
        where TInterface : class
    {
        var entries = messageTypesByInterface.GetOrAdd(typeof(TInterface),
                                                       _ => (from r in registrationByMessageType.Values
                                                             where r.MessageType.IsAssignableTo(typeof(TInterface))
                                                             select (r, r.TypeInjectors.FirstOrDefault(i => i.ConstraintType == typeof(TInterface))))
                                                           .ToList());

        return entries.Select(e => (e.Registration.MessageType, e.Registration.ResponseType, e.TypeInjector)).ToList();
    }

    public MessageHandlerRegistration? GetMessageHandlerRegistration(Type messageType)
    {
        return registrationByMessageType.GetValueOrDefault(messageType);
    }
}

public sealed record MessageHandlerRegistration(
    Type MessageType,
    Type ResponseType,
    Type HandlerType,
    Type? HandlerAdapterType,
    Delegate? ConfigurePipeline,
    IReadOnlyCollection<IMessageTypesInjector> TypeInjectors);
