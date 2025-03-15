using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror;

internal sealed class MessageTransportRegistry : IMessageTransportRegistry
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type MessageType, Type ResponseType, object Attribute)>> messageTypesByTransportAttribute = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type MessageType, Type ResponseType)>> messageTypesByInterface = new();
    private readonly Dictionary<Type, MessageHandlerRegistration> registrationByMessageType;

    public MessageTransportRegistry(IEnumerable<MessageHandlerRegistration> registrations)
    {
        registrationByMessageType = registrations.ToDictionary(r => r.MessageType);
    }

    public IReadOnlyCollection<(Type MessageType, Type ResponseType, TTransportMarkerAttribute Attribute)> GetMessageTypesForTransport<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : Attribute
    {
        var entries = messageTypesByTransportAttribute.GetOrAdd(typeof(TTransportMarkerAttribute),
                                                                _ => (from r in registrationByMessageType.Values
                                                                      let attribute = r.MessageType.GetCustomAttribute<TTransportMarkerAttribute>()
                                                                      where attribute != null || typeof(TTransportMarkerAttribute) == typeof(InProcessMessageAttribute)
                                                                      select (r.MessageType, r.ResponseType, (object)attribute ?? new InProcessMessageAttribute())).ToList());

        return entries.Select(e => (e.MessageType, e.ResponseType, (TTransportMarkerAttribute)e.Attribute)).ToList();
    }

    public IReadOnlyCollection<(Type MessageType, Type ResponseType)> GetMessageTypesForTransportInterface<TInterface>()
        where TInterface : class
    {
        var entries = messageTypesByInterface.GetOrAdd(typeof(TInterface),
                                                       _ => (from r in registrationByMessageType.Values
                                                             where r.MessageType.IsAssignableTo(typeof(TInterface))
                                                             select (r.MessageType, r.ResponseType)).ToList());

        return entries.Select(e => (e.MessageType, e.ResponseType)).ToList();
    }

    public MessageHandlerRegistration? GetMessageHandlerRegistration(Type messageType)
    {
        return registrationByMessageType.GetValueOrDefault(messageType);
    }
}

public sealed record MessageHandlerRegistration(Type MessageType, Type ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
