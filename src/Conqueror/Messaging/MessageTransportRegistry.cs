using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Messaging;

internal sealed class MessageTransportRegistry(IEnumerable<MessageHandlerRegistration> registrations) : IMessageTransportRegistry
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(MessageHandlerRegistration Registration, IMessageTypesInjector TypeInjector)>> messageTypesByInjectorType = new();
    private readonly Dictionary<Type, MessageHandlerRegistration> registrationByMessageType = registrations.ToDictionary(r => r.MessageType);

    public IReadOnlyCollection<(Type MessageType, Type ResponseType, TTypesInjector TypesInjector)> GetMessageTypesForTransport<TTypesInjector>()
        where TTypesInjector : class, IMessageTypesInjector
    {
        var entries = messageTypesByInjectorType.GetOrAdd(typeof(TTypesInjector),
                                                          _ => (from r in registrationByMessageType.Values
                                                                let typeInjector = r.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault()
                                                                where typeInjector is not null
                                                                select (r, (IMessageTypesInjector)typeInjector))
                                                              .ToList());

        return entries.Select(e => (e.Registration.MessageType, e.Registration.ResponseType, (TTypesInjector)e.TypeInjector)).ToList();
    }

    public MessageHandlerRegistration? GetMessageHandlerRegistration(Type messageType)
        => registrationByMessageType.GetValueOrDefault(messageType);
}

public sealed record MessageHandlerRegistration(
    Type MessageType,
    Type ResponseType,
    Type HandlerType,
    Type? HandlerAdapterType,
    Delegate? ConfigurePipeline,
    IReadOnlyCollection<IMessageTypesInjector> TypeInjectors);
