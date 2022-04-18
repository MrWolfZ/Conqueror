using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS
{
    internal static class CqsQueryHandlerTypeExtensions
    {
        public static (Type QueryType, Type ResponseType) GetQueryAndResponseType(this Type type)
        {
            var interfaceType = GetQueryHandlerInterfaceType(type);
            var queryType = interfaceType.GetGenericArguments().First();
            var responseType = interfaceType.GetGenericArguments().Skip(1).First();
            return (queryType, responseType);
        }

        public static Type GetQueryHandlerInterfaceType(this Type type)
        {
            var interfaces = type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsQueryHandlerInterfaceType()).ToList();

            return interfaces.Count switch
            {
                < 1 => throw new ArgumentException($"type {type.Name} does not implement a generic query handler interface"),
                > 1 => throw new ArgumentException($"type {type.Name} implements more than one query handler interface"),
                _ => interfaces.Single(),
            };
        }

        public static Type? GetCustomQueryHandlerInterfaceType(this Type type)
        {
            var interfaces = type.GetInterfaces().Where(i => i.IsCustomQueryHandlerInterfaceType()).ToList();

            var customInterface = interfaces.Count switch
            {
                < 1 => null,
                > 1 => throw new ArgumentException($"type {type.Name} implements more than one custom query handler interface"),
                _ => interfaces.Single(),
            };

            if (customInterface != null && customInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException($"type {type.Name} implements custom interface {customInterface.Name} that has extra methods");
            }

            return customInterface;
        }

        public static bool IsCustomQueryHandlerInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsQueryHandlerInterfaceType);

        public static bool IsQueryHandlerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);

        private static IEnumerable<MethodInfo> AllMethods(this Type t) => t.GetInterfaces().Concat(new[] { t }).SelectMany(s => s.GetMethods());
    }
}
