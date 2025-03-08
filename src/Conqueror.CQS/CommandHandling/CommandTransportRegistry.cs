using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandTransportRegistry : ICommandTransportRegistry
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type CommandType, Type? ResponseType, object Attribute)>> commandTypesByTransportAttribute = new();
    private readonly Dictionary<Type, CommandHandlerRegistration> registrationByCommandType;

    public CommandTransportRegistry(IEnumerable<CommandHandlerRegistration> registrations)
    {
        registrationByCommandType = registrations.ToDictionary(r => r.CommandType);
    }

    public IReadOnlyCollection<(Type CommandType, Type? ResponseType, TTransportMarkerAttribute Attribute)> GetCommandTypesForTransport<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : Attribute
    {
        var entries = commandTypesByTransportAttribute.GetOrAdd(typeof(TTransportMarkerAttribute),
                                                                _ => (from r in registrationByCommandType.Values
                                                                      let attribute = r.CommandType.GetCustomAttribute<TTransportMarkerAttribute>()
                                                                      where attribute != null || typeof(TTransportMarkerAttribute) == typeof(InProcessCommandAttribute)
                                                                      select (r.CommandType, r.ResponseType, (object)attribute ?? new InProcessCommandAttribute())).ToList());

        return entries.Select(e => (e.CommandType, e.ResponseType, (TTransportMarkerAttribute)e.Attribute)).ToList();
    }

    public CommandHandlerRegistration? GetCommandHandlerRegistration(Type queryType)
    {
        return registrationByCommandType.GetValueOrDefault(queryType);
    }
}

public sealed record CommandHandlerRegistration(Type CommandType, Type? ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
