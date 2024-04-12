using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;

namespace Conqueror.CQS.QueryHandling;

internal static class QueryHandlerTypeExtensions
{
    public static IReadOnlyCollection<(Type QueryType, Type ResponseType)> GetQueryAndResponseTypes(this Type type)
    {
        return GetQueryHandlerInterfaceTypes(type).Select(t =>
        {
            var queryType = t.GetGenericArguments()[0];
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
        if (!type.IsQueryHandlerInterfaceType())
        {
            var interfaces = type.GetInterfaces();
            if (interfaces.Length == 1 && interfaces[0] == typeof(IQueryHandler))
            {
                throw new ArgumentException($"type {type.Name} implements non-generic query handler interface {nameof(IQueryHandler)}");
            }
        }

        var invalidInterface = type
                               .GetInterfaces()
                               .Concat(new[] { type })
                               .Where(i => i.IsCustomQueryHandlerInterfaceType())
                               .FirstOrDefault(i => i.AllMethods().Count() > 1);

        if (invalidInterface != null)
        {
            throw new ArgumentException(
                $"query handler interface type '{invalidInterface.Name}' has extra methods; custom query handler interface types are not allowed to have any additional methods beside the '{nameof(IQueryHandler<object, object>.ExecuteQuery)}' inherited from '{typeof(IQueryHandler<,>).Name}'");
        }
    }

    public static bool IsCustomQueryHandlerInterfaceType(this Type t) => t.IsInterface && Array.Exists(t.GetInterfaces(), IsQueryHandlerInterfaceType);

    public static bool IsCustomQueryHandlerInterfaceType<TQuery, TResponse>(this Type t)
        where TQuery : class =>
        t.IsInterface && Array.Exists(t.GetInterfaces(), i => i == typeof(IQueryHandler<TQuery, TResponse>));

    public static bool IsQueryHandlerInterfaceType(this Type t) => t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);
}
