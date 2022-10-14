using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror
{
    internal static class CqsCommandHandlerTypeExtensions
    {
        public static IReadOnlyCollection<(Type CommandType, Type? ResponseType)> GetCommandAndResponseTypes(this Type type)
        {
            return GetCommandHandlerInterfaceTypes(type).Select(t =>
            {
                var queryType = t.GetGenericArguments().First();
                var responseType = t.GetGenericArguments().Skip(1).FirstOrDefault();
                return (queryType, responseType);
            }).ToList();
        }

        public static IReadOnlyCollection<Type> GetCommandHandlerInterfaceTypes(this Type type)
        {
            return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsCommandHandlerInterfaceType()).ToList();
        }

        public static IReadOnlyCollection<Type> GetCustomCommandHandlerInterfaceTypes(this Type type)
        {
            var interfaces = type.GetInterfaces().Where(i => i.IsCustomCommandHandlerInterfaceType()).ToList();

            var invalidInterface = interfaces.FirstOrDefault(i => i.AllMethods().Count() > 1);
            if (invalidInterface is not null)
            {
                throw new ArgumentException($"type {type.Name} implements custom interface {invalidInterface.Name} that has extra methods");
            }

            return interfaces;
        }

        public static void ValidateNoInvalidCommandHandlerInterface(this Type type)
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(ICommandHandler))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic command handler interface {nameof(ICommandHandler)}");
            }
        }

        public static bool IsCustomCommandHandlerInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsCommandHandlerInterfaceType);

        public static bool IsCommandHandlerInterfaceType(this Type t) =>
            t.IsInterface && t.IsGenericType
                          && (t.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                              || t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

        private static IEnumerable<MethodInfo> AllMethods(this Type t) => t.GetInterfaces().Concat(new[] { t }).SelectMany(s => s.GetMethods());
    }
}
