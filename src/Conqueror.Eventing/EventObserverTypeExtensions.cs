using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.Eventing;

internal static class EventObserverTypeExtensions
{
    public static IReadOnlyCollection<Type> GetObservedEventTypes(this Type type)
    {
        return GetEventObserverInterfaceTypes(type).Select(t => t.GetGenericArguments()[0]).ToList();
    }

    public static IReadOnlyCollection<Type> GetEventObserverInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsEventObserverInterfaceType()).ToList();
    }

    public static Type GetPublisherConfigurationAttributeType(this Type publisherType)
    {
        return GetPublisherInterfaceTypes(publisherType).First().GetGenericArguments()[0];
    }

    public static IReadOnlyCollection<Type> GetPublisherInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsEventPublisherInterfaceType()).ToList();
    }

    public static void ValidateNoInvalidEventObserverInterface(this Type type)
    {
        if (!type.IsEventObserverInterfaceType())
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(IEventObserver))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic event observer interface {nameof(IEventObserver)}");
            }
        }

        var invalidInterface = type.GetInterfaces()
                                   .Concat(new[] { type })
                                   .Where(i => i.IsCustomEventObserverInterfaceType())
                                   .FirstOrDefault(i => i.AllMethods().Count() > 1);

        if (invalidInterface != null)
        {
            throw new ArgumentException(
                $"event observer interface type '{invalidInterface.Name}' has extra methods; custom event observer interface types are not allowed to have any additional methods beside the '{nameof(IEventObserver<object>.HandleEvent)}' inherited from '{typeof(IEventObserver<>).Name}'");
        }
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

    public static bool IsCustomEventObserverInterfaceType(this Type t) => t.IsInterface && Array.Exists(t.GetInterfaces(), IsEventObserverInterfaceType);

    public static bool IsCustomEventObserverInterfaceType<TEvent>(this Type t)
        where TEvent : class =>
        t.IsInterface && Array.Exists(t.GetInterfaces(), i => i == typeof(IEventObserver<TEvent>));

    public static bool IsEventObserverInterfaceType(this Type t) => t is { IsInterface: true, IsGenericType: true } && t.GetGenericTypeDefinition() == typeof(IEventObserver<>);

    public static bool IsEventPublisherInterfaceType(this Type t) => t is { IsInterface: true, IsGenericType: true } && t.GetGenericTypeDefinition() == typeof(IConquerorEventTransportPublisher<>);
}
