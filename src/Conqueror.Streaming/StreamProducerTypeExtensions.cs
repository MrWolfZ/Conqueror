using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.Streaming;

internal static class StreamProducerTypeExtensions
{
    public static IReadOnlyCollection<(Type RequestType, Type ItemType)> GetStreamProducerRequestAndItemTypes(this Type type)
    {
        return GetStreamProducerInterfaceTypes(type).Select(t =>
        {
            var requestType = t.GetGenericArguments()[0];
            var itemType = t.GetGenericArguments().Skip(1).First();
            return (requestType, itemType);
        }).ToList();
    }

    public static IReadOnlyCollection<Type> GetStreamProducerInterfaceTypes(this Type type)
    {
        return type.GetInterfaces().Concat([type]).Where(i => i.IsStreamProducerInterfaceType()).ToList();
    }

    public static void ValidateNoInvalidStreamProducerInterface(this Type type)
    {
        if (!type.IsStreamProducerInterfaceType())
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(IStreamProducer))
            {
                throw new ArgumentException($"type '{type.Name}' implements non-generic stream producer interface '{nameof(IStreamProducer)}'");
            }
        }

        var invalidInterface = type
                               .GetInterfaces()
                               .Concat(new[] { type })
                               .Where(i => i.IsCustomStreamProducerInterfaceType())
                               .FirstOrDefault(i => i.AllMethods().Count() > 1);

        if (invalidInterface != null)
        {
            throw new ArgumentException($"stream producer interface type '{invalidInterface.Name}' has extra methods; custom stream producer interface types are not allowed to have any additional methods beside the '{nameof(IStreamProducer<object, object>.ExecuteRequest)}' method inherited from '{typeof(IStreamProducer<,>).Name}'");
        }
    }

    public static bool IsCustomStreamProducerInterfaceType(this Type t) => t.IsInterface && Array.Exists(t.GetInterfaces(), IsStreamProducerInterfaceType);

    public static bool IsCustomStreamProducerInterfaceType<TRequest, TItem>(this Type t)
        where TRequest : class =>
        t.IsInterface && Array.Exists(t.GetInterfaces(), i => i == typeof(IStreamProducer<TRequest, TItem>));

    public static bool IsStreamProducerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IStreamProducer<,>);
}
