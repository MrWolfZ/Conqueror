using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Streaming;

internal static class StreamingHandlerTypeExtensions
{
    public static IReadOnlyCollection<(Type RequestType, Type ItemType)> GetStreamingRequestAndItemTypes(this Type type)
    {
        return GetStreamingHandlerInterfaceTypes(type).Select(t =>
        {
            var requestType = t.GetGenericArguments()[0];
            var itemType = t.GetGenericArguments().Skip(1).First();
            return (requestType, itemType);
        }).ToList();
    }

    public static IReadOnlyCollection<Type> GetStreamingHandlerInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsStreamingHandlerInterfaceType()).ToList();
    }

    public static IReadOnlyCollection<Type> GetCustomStreamingHandlerInterfaceTypes(this Type type)
    {
        var interfaces = type.GetInterfaces().Where(i => i.IsCustomStreamingHandlerInterfaceType()).ToList();

        var invalidInterface = interfaces.Find(i => i.AllMethods().Count() > 1);
        if (invalidInterface is not null)
        {
            throw new ArgumentException($"type '{type.Name}' implements custom interface '{invalidInterface.Name}' that has extra methods");
        }

        return interfaces;
    }

    public static void ValidateNoInvalidStreamingHandlerInterface(this Type type)
    {
        var interfaces = type.GetInterfaces();
        if (interfaces.Length == 1 && interfaces[0] == typeof(IStreamingHandler))
        {
            throw new ArgumentException($"type '{type.Name}' implements non-generic streaming handler interface '{nameof(IStreamingHandler)}'");
        }
    }

    public static bool IsCustomStreamingHandlerInterfaceType(this Type t) => t.IsInterface && Array.Exists(t.GetInterfaces(), IsStreamingHandlerInterfaceType);

    public static bool IsStreamingHandlerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IStreamingHandler<,>);

    private static IEnumerable<MethodInfo> AllMethods(this Type t) => t.GetInterfaces().Concat(new[] { t }).SelectMany(s => s.GetMethods());
}
