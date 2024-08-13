using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.Streaming;

internal static class StreamingRequestHandlerTypeExtensions
{
    public static IReadOnlyCollection<(Type RequestType, Type ItemType)> GetStreamingRequestAndItemTypes(this Type type)
    {
        return GetStreamingRequestHandlerInterfaceTypes(type).Select(t =>
        {
            var requestType = t.GetGenericArguments()[0];
            var itemType = t.GetGenericArguments().Skip(1).First();
            return (requestType, itemType);
        }).ToList();
    }

    public static IReadOnlyCollection<Type> GetStreamingRequestHandlerInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsStreamingRequestHandlerInterfaceType()).ToList();
    }

    public static void ValidateNoInvalidStreamingRequestHandlerInterface(this Type type)
    {
        if (!type.IsStreamingRequestHandlerInterfaceType())
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(IStreamingRequestHandler))
            {
                throw new ArgumentException($"type '{type.Name}' implements non-generic streaming handler interface '{nameof(IStreamingRequestHandler)}'");
            }
        }

        var invalidInterface = type
                               .GetInterfaces()
                               .Concat(new[] { type })
                               .Where(i => i.IsCustomStreamingRequestHandlerInterfaceType())
                               .FirstOrDefault(i => i.AllMethods().Count() > 1);

        if (invalidInterface != null)
        {
            throw new ArgumentException($"streaming request handler interface type '{invalidInterface.Name}' has extra methods; custom streaming request handler interface types are not allowed to have any additional methods beside the '{nameof(IStreamingRequestHandler<object, object>.ExecuteRequest)}' method inherited from '{typeof(IStreamingRequestHandler<,>).Name}'");
        }
    }

    public static bool IsCustomStreamingRequestHandlerInterfaceType(this Type t) => t.IsInterface && Array.Exists(t.GetInterfaces(), IsStreamingRequestHandlerInterfaceType);

    public static bool IsCustomStreamingRequestHandlerInterfaceType<TRequest, TItem>(this Type t)
        where TRequest : class =>
        t.IsInterface && Array.Exists(t.GetInterfaces(), i => i == typeof(IStreamingRequestHandler<TRequest, TItem>));

    public static bool IsStreamingRequestHandlerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IStreamingRequestHandler<,>);
}
