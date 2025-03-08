using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Eventing.Publishing;

internal static class EventPublisherTypeExtensions
{
    public static IReadOnlyCollection<Type> GetPublisherConfigurationAttributeTypes(this Type publisherType)
    {
        return GetPublisherInterfaceTypes(publisherType).Select(i => i.GetGenericArguments()[0]).ToList();
    }

    public static void ValidateNoInvalidEventPublisherInterface(this Type type)
    {
        if (!type.IsEventPublisherInterfaceType())
        {
            var interfaces = type.GetInterfaces();

            if (interfaces.Length == 1 && interfaces[0] == typeof(IEventTransportPublisher))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic event publisher interface {nameof(IEventTransportPublisher)}");
            }
        }
    }

    private static IReadOnlyCollection<Type> GetPublisherInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat([type]).Where(i => i.IsEventPublisherInterfaceType()).ToList();
    }

    private static bool IsEventPublisherInterfaceType(this Type t) => t is { IsInterface: true, IsGenericType: true } && t.GetGenericTypeDefinition() == typeof(IEventTransportPublisher<>);
}
