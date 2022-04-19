using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.Eventing.Util
{
    internal static class TypeExtensions
    {
        public static IReadOnlyCollection<Type> GetCustomEventObserverInterfaceTypes(this Type observerType)
        {
            var interfaces = observerType.GetInterfaces().Where(i => i.IsCustomEventObserverInterfaceType()).ToList();

            if (interfaces.FirstOrDefault(i => i.AllMethods().Count() > 1) is { } t)
            {
                throw new ArgumentException($"type {observerType.Name} implements custom interface {t.Name} that has extra methods");
            }

            return interfaces;
        }

        public static IReadOnlyCollection<Type> GetEventObserverInterfaceTypes(this Type observerType)
        {
            var interfaces = observerType.GetInterfaces().Concat(new[] { observerType }).Where(i => i.IsEventObserverInterfaceType()).ToList();

            return interfaces.Count switch
            {
                < 1 => throw new ArgumentException($"type {observerType.Name} does not implement a generic event observer interface"),
                _ => interfaces,
            };
        }

        public static bool IsCustomEventObserverInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsEventObserverInterfaceType);

        public static bool IsEventObserverInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEventObserver<>);
    }
}
