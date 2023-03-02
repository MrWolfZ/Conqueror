using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client;

internal static class InteractiveStreamingHandlerTypeExtensions
{
    public static IReadOnlyCollection<(Type RequestType, Type ItemType)> GetInteractiveStreamingRequestAndItemTypes(this Type type)
    {
        return GetInteractiveStreamingHandlerInterfaceTypes(type).Select(t =>
        {
            var requestType = t.GetGenericArguments().First();
            var itemType = t.GetGenericArguments().Skip(1).First();
            return (requestType, itemType);
        }).ToList();
    }

    public static IReadOnlyCollection<Type> GetInteractiveStreamingHandlerInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsInteractiveStreamingHandlerInterfaceType()).ToList();
    }

    public static IReadOnlyCollection<Type> GetCustomInteractiveStreamingHandlerInterfaceTypes(this Type type)
    {
        var interfaces = type.GetInterfaces().Where(i => i.IsCustomInteractiveStreamingHandlerInterfaceType()).ToList();

        var invalidInterface = interfaces.FirstOrDefault(i => i.AllMethods().Count() > 1);
        if (invalidInterface is not null)
        {
            throw new ArgumentException($"type '{type.Name}' implements custom interface '{invalidInterface.Name}' that has extra methods");
        }

        return interfaces;
    }

    public static void ValidateNoInvalidInteractiveStreamingHandlerInterface(this Type type)
    {
        var interfaces = type.GetInterfaces();
        if (interfaces.Length == 1 && interfaces[0] == typeof(IInteractiveStreamingHandler))
        {
            throw new ArgumentException($"type '{type.Name}' implements non-generic interactive streaming handler interface '{nameof(IInteractiveStreamingHandler)}'");
        }
    }

    public static bool IsCustomInteractiveStreamingHandlerInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsInteractiveStreamingHandlerInterfaceType);

    public static bool IsInteractiveStreamingHandlerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IInteractiveStreamingHandler<,>);

    private static IEnumerable<MethodInfo> AllMethods(this Type t) => t.GetInterfaces().Concat(new[] { t }).SelectMany(s => s.GetMethods());
}
