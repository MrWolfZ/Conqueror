using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS
{
    internal static class CqsCommandHandlerTypeExtensions
    {
        public static (Type CommandType, Type? ResponseType) GetCommandAndResponseType(this Type type)
        {
            var interfaceType = GetCommandHandlerInterfaceType(type);
            var queryType = interfaceType.GetGenericArguments().First();
            var responseType = interfaceType.GetGenericArguments().Skip(1).FirstOrDefault();
            return (queryType, responseType);
        }

        public static Type GetCommandHandlerInterfaceType(this Type type)
        {
            var interfaces = type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsCommandHandlerInterfaceType()).ToList();

            return interfaces.Count switch
            {
                < 1 => throw new ArgumentException($"type {type.Name} does not implement a generic command handler interface"),
                > 1 => throw new ArgumentException($"type {type.Name} implements more than one command handler interface"),
                _ => interfaces.Single(),
            };
        }

        public static Type? GetCustomCommandHandlerInterfaceType(this Type type)
        {
            var interfaces = type.GetInterfaces().Where(i => i.IsCustomCommandHandlerInterfaceType()).ToList();

            var customInterface = interfaces.Count switch
            {
                < 1 => null,
                > 1 => throw new ArgumentException($"type {type.Name} implements more than one custom command handler interface"),
                _ => interfaces.Single(),
            };

            if (customInterface != null && customInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException($"type {type.Name} implements custom interface {customInterface.Name} that has extra methods");
            }

            return customInterface;
        }

        public static bool IsCustomCommandHandlerInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsCommandHandlerInterfaceType);

        public static bool IsCommandHandlerInterfaceType(this Type t) =>
            t.IsInterface && t.IsGenericType
                          && (t.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                              || t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

        private static IEnumerable<MethodInfo> AllMethods(this Type t) => t.GetInterfaces().Concat(new[] { t }).SelectMany(s => s.GetMethods());
    }
}
