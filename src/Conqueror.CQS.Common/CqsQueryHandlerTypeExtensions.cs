using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.Common
{
    internal static class CqsQueryHandlerTypeExtensions
    {
        public static IReadOnlyCollection<(Type QueryType, Type ResponseType)> GetQueryAndResponseTypes(this Type type)
        {
            return GetQueryHandlerInterfaceTypes(type).Select(t =>
            {
                var queryType = t.GetGenericArguments().First();
                var responseType = t.GetGenericArguments().Skip(1).First();
                return (queryType, responseType);
            }).ToList();
        }

        public static IReadOnlyCollection<Type> GetQueryHandlerInterfaceTypes(this Type type)
        {
            return type.GetInterfaces().Concat(new[] { type }).Where(i => i.IsQueryHandlerInterfaceType()).ToList();
        }

        public static void ValidateNoInvalidQueryHandlerInterface(this Type type)
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(IQueryHandler))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic query handler interface {nameof(IQueryHandler)}");
            }
        }

        public static bool IsCustomQueryHandlerInterfaceType(this Type t) => t.IsInterface && t.GetInterfaces().Any(IsQueryHandlerInterfaceType);

        public static bool IsCustomQueryHandlerInterfaceType<TQuery, TResponse>(this Type t)
            where TQuery : class =>
            t.IsInterface && t.GetInterfaces().Any(i => i == typeof(IQueryHandler<TQuery, TResponse>));

        public static bool IsQueryHandlerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);
    }
}
