using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.Streaming;

internal static class StreamConsumerTypeExtensions
{
    public static IReadOnlyCollection<Type> GetStreamConsumerItemTypes(this Type type)
    {
        return GetStreamConsumerInterfaceTypes(type).Select(t => t.GetGenericArguments()[0]).ToList();
    }

    public static IReadOnlyCollection<Type> GetStreamConsumerInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat([type]).Where(i => i.IsStreamConsumerInterfaceType()).ToList();
    }

    public static void ValidateNoInvalidStreamConsumerInterface(this Type type)
    {
        if (!type.IsStreamConsumerInterfaceType())
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(IStreamConsumer))
            {
                throw new ArgumentException($"type '{type}' implements non-generic stream consumer interface '{nameof(IStreamConsumer)}'");
            }
        }

        var invalidInterface = type.GetInterfaces()
                                   .Concat([type])
                                   .Where(i => i.IsCustomStreamConsumerInterfaceType())
                                   .FirstOrDefault(i => i.AllMethods().Count() > 1);

        if (invalidInterface != null)
        {
            throw new ArgumentException($"stream consumer interface type '{invalidInterface}' has extra methods; custom stream consumer interface types are not allowed to have any additional methods beside the '{nameof(IStreamConsumer<object>.HandleItem)}' method inherited from '{typeof(IStreamConsumer<>).Name}'");
        }
    }

    public static bool IsCustomStreamConsumerInterfaceType(this Type t) => t.IsInterface && Array.Exists(t.GetInterfaces(), IsStreamConsumerInterfaceType);

    public static bool IsCustomStreamConsumerInterfaceType<TItem>(this Type t) =>
        t.IsInterface && Array.Exists(t.GetInterfaces(), i => i == typeof(IStreamConsumer<TItem>));

    public static bool IsStreamConsumerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IStreamConsumer<>);

    public static bool IsStreamConsumerConcreteType(this Type t) => !t.IsInterface && !t.IsAbstract && t.GetStreamConsumerInterfaceTypes().Count > 0;
}
