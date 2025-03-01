using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Eventing.Publishing;

internal static class EventPublisherTypeExtensions
{
    public static Type GetPublisherConfigurationAttributeType(this Type publisherType)
    {
        return GetPublisherInterfaceTypes(publisherType).First().GetGenericArguments()[0];
    }

    public static IReadOnlyCollection<Type> GetPublisherInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat([type]).Where(i => i.IsEventPublisherInterfaceType()).ToList();
    }

    public static void ValidateNoInvalidEventPublisherInterface(this Type type)
    {
        if (!type.IsEventPublisherInterfaceType())
        {
            var interfaces = type.GetInterfaces();

            if (interfaces.Length == 1 && interfaces[0] == typeof(IConquerorEventTransportPublisher))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic event publisher interface {nameof(IConquerorEventTransportPublisher)}");
            }

            var publisherInterfacesCount = interfaces.Count(i => i.IsEventPublisherInterfaceType());

            if (publisherInterfacesCount > 1)
            {
                throw new ArgumentException($"type {type.Name} implements multiple publisher interfaces, but is only allowed to implement a single interface");
            }
        }
    }

    public static bool IsEventPublisherInterfaceType(this Type t) => t is { IsInterface: true, IsGenericType: true } && t.GetGenericTypeDefinition() == typeof(IConquerorEventTransportPublisher<>);
}
